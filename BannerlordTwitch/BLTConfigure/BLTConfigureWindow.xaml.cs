using System;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using BannerlordTwitch;
using BLTConfigure.UI;
using Newtonsoft.Json;

namespace BLTConfigure
{
    public class NewActionViewModel
    {
        public string Module => NewType.Assembly.GetName().Name;
        public string Name => NewType.Name;
        public string Description => NewType.GetCustomAttribute<DescriptionAttribute>()?.Description;
        public System.Windows.Input.ICommand Command { get; }
        public Type NewType { get; }
        public NewActionViewModel(Action<object> command, Type newType) { NewType = newType; Command = new RelayCommand(command); }
    }

    public partial class BLTConfigureWindow : Window, INotifyPropertyChanged
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        private readonly DispatcherTimer pollingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        private bool requestInFlight;
        public ConfigurationRootViewModel ConfigurationRoot { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public BLTConfigureWindow()
        {
            InitializeComponent(); ConfigurationRoot = new ConfigurationRootViewModel(); DataContext = this;
            PairingCodeTextBox.Text = ConfigurationRoot.EditedAuthSettings.IntegrationPairingCode ?? string.Empty;
            pollingTimer.Tick += async (_, __) => await PollPairingStatusAsync();
            RefreshAuthorizationStatus(); RefreshPairingStatus();
        }

        public class TypeGroupDescription : GroupDescription
        {
            public override object GroupNameFromItem(object item, int level, CultureInfo culture) => item switch { null => "", GlobalConfig => "Global Configs", Reward => "Channel Rewards", Command => "Chat Commands", SimTestingConfig => "Sim Testing Config", _ => item.GetType().Name };
        }

        private static readonly string[] MainScopes = { "user:read:email", "chat:edit", "chat:read", "whispers:read", "channel:read:subscriptions", "channel:read:redemptions", "channel:manage:redemptions" };
        private static readonly string[] BotScopes = MainScopes;

        private async void GenerateToken_OnClick(object sender, RoutedEventArgs e)
        {
            try { GenerateTokenButton.IsEnabled = false; GenerateTokenCancel.Visibility = Visibility.Visible; ConfigurationRoot.EditedAuthSettings.AccessToken = await TwitchAuthHelper.Authorize(MainScopes) ?? throw new AuthenticationException("No token was returned."); if (string.IsNullOrWhiteSpace(ConfigurationRoot.EditedAuthSettings.BotAccessToken)) ConfigurationRoot.EditedAuthSettings.BotAccessToken = ConfigurationRoot.EditedAuthSettings.AccessToken; ConfigurationRoot.SaveAuth(); }
            catch (Exception ex) { BroadcasterStatus.Text = $"Authorization failed: {ex.Message}"; }
            finally { GenerateTokenButton.IsEnabled = true; GenerateTokenCancel.Visibility = Visibility.Collapsed; RefreshAuthorizationStatus(); }
        }

        private async void GenerateBotToken_OnClick(object sender, RoutedEventArgs e)
        {
            try { GenerateBotTokenButton.IsEnabled = false; GenerateBotTokenCancel.Visibility = Visibility.Visible; ConfigurationRoot.EditedAuthSettings.BotAccessToken = await TwitchAuthHelper.Authorize(BotScopes) ?? throw new AuthenticationException("No token was returned."); ConfigurationRoot.SaveAuth(); }
            catch (Exception ex) { BotStatus.Text = $"Bot authorization failed: {ex.Message}"; }
            finally { GenerateBotTokenButton.IsEnabled = true; GenerateBotTokenCancel.Visibility = Visibility.Collapsed; RefreshAuthorizationStatus(); }
        }

        private void CancelAuth_OnClick(object sender, RoutedEventArgs e) => TwitchAuthHelper.CancelAuth();
        private void UseMainAccountForBot_OnClick(object sender, RoutedEventArgs e) { ConfigurationRoot.EditedAuthSettings.BotAccessToken = ConfigurationRoot.EditedAuthSettings.AccessToken; ConfigurationRoot.SaveAuth(); RefreshAuthorizationStatus(); }

        private async void Pair_OnClick(object sender, RoutedEventArgs e)
        {
            if (requestInFlight) return;
            var existing = ConfigurationRoot.EditedAuthSettings;
            if (!string.IsNullOrWhiteSpace(existing.IntegrationPairingRequestId) && !string.IsNullOrWhiteSpace(existing.IntegrationPairingRequestToken))
            {
                PairButton.Content = "Checking…";
                await PollPairingStatusAsync();
                if (!string.IsNullOrWhiteSpace(existing.IntegrationPairingRequestId)) pollingTimer.Start();
                return;
            }
            string code = PairingCodeTextBox.Text.Trim().ToUpperInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(code, "^BLT-[A-F0-9]{4}-[A-F0-9]{4}$")) { SetPairingState("Invalid code", "Paste the complete code shown in Twitch Config."); return; }
            requestInFlight = true; PairButton.IsEnabled = false;
            try
            {
                ClearPendingAuthentication();
                var payload = new { code, modVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown", platformLabel = "Windows PC", fingerprint = CreateFingerprint() };
                using var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await Http.PostAsync(ServiceUrl + "/api/pairing/requests", content); string json = await response.Content.ReadAsStringAsync(); response.EnsureSuccessStatusCode();
                var receipt = JsonConvert.DeserializeObject<PairingReceipt>(json); var auth = ConfigurationRoot.EditedAuthSettings;
                auth.IntegrationServiceUrl = ServiceUrl; auth.IntegrationPairingCode = code; auth.IntegrationPairingRequestId = receipt.RequestId; auth.IntegrationPairingRequestToken = receipt.RequestToken; auth.IntegrationCandidateCredential = receipt.CandidateCredential; auth.IntegrationPairingExpiresAt = receipt.ExpiresAt;
                ConfigurationRoot.SaveAuth(); SetPairingState("Waiting for broadcaster approval", "Return to Twitch Config, choose Accept or Deny, then press Save."); PairButton.Visibility = Visibility.Collapsed; CancelPairingButton.Visibility = Visibility.Visible; pollingTimer.Start();
            }
            catch (Exception ex) { ClearPendingAuthentication(); SetPairingState("Pairing failed", ex.Message); }
            finally { requestInFlight = false; PairButton.IsEnabled = true; }
        }

        private async Task PollPairingStatusAsync()
        {
            var auth = ConfigurationRoot.EditedAuthSettings;
            if (requestInFlight || string.IsNullOrWhiteSpace(auth.IntegrationPairingRequestId) || string.IsNullOrWhiteSpace(auth.IntegrationPairingRequestToken)) return;
            requestInFlight = true;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, ServiceUrl + "/api/pairing/requests/" + Uri.EscapeDataString(auth.IntegrationPairingRequestId) + "/status"); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.IntegrationPairingRequestToken);
                var response = await Http.SendAsync(request); string json = await response.Content.ReadAsStringAsync(); response.EnsureSuccessStatusCode(); var status = JsonConvert.DeserializeObject<PairingStatus>(json);
                if (status.Status == "approved") { auth.IntegrationChannelId = status.ChannelId; auth.IntegrationInstallationId = status.InstallationId; auth.IntegrationCredential = auth.IntegrationCandidateCredential; ClearPendingAuthentication(false); ConfigurationRoot.SaveAuth(); pollingTimer.Stop(); SetPairingState("Paired and approved", "This installation can now connect to the managed BLT service."); RefreshPairingStatus(); }
                else if (status.Status == "denied" || status.Status == "expired" || status.Status == "cancelled") { ClearPendingAuthentication(); pollingTimer.Stop(); SetPairingState(status.Status == "denied" ? "Pairing denied" : "Pairing expired", "Generate a new code in Twitch Config when you are ready to try again."); }
            }
            catch (Exception ex) { SetPairingState("Waiting to reconnect", ex.Message); }
            finally { requestInFlight = false; }
        }

        private async void CancelPairing_OnClick(object sender, RoutedEventArgs e)
        {
            var auth = ConfigurationRoot.EditedAuthSettings;
            try { if (!string.IsNullOrWhiteSpace(auth.IntegrationPairingRequestId) && !string.IsNullOrWhiteSpace(auth.IntegrationPairingRequestToken)) { using var request = new HttpRequestMessage(HttpMethod.Delete, ServiceUrl + "/api/pairing/requests/" + Uri.EscapeDataString(auth.IntegrationPairingRequestId)); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.IntegrationPairingRequestToken); await Http.SendAsync(request); } }
            finally { pollingTimer.Stop(); ClearPendingAuthentication(); SetPairingState("Request cancelled", "No credential was activated."); }
        }

        private void Repair_OnClick(object sender, RoutedEventArgs e) { var auth = ConfigurationRoot.EditedAuthSettings; auth.IntegrationCredential = null; auth.IntegrationInstallationId = null; auth.IntegrationChannelId = null; ConfigurationRoot.SaveAuth(); PairingCodeTextBox.Clear(); RefreshPairingStatus(); }
        private void PairingCode_OnTextChanged(object sender, TextChangedEventArgs e) { if (PairButton != null) PairButton.IsEnabled = !requestInFlight && !string.IsNullOrWhiteSpace(PairingCodeTextBox.Text); }
        private void RefreshAuthorizationStatus() { bool authorized = !string.IsNullOrWhiteSpace(ConfigurationRoot.EditedAuthSettings.AccessToken); BroadcasterStatus.Text = authorized ? "Broadcaster account authorized." : "Authorization is required for chat, rewards, and channel identification."; bool separateBot = !string.IsNullOrWhiteSpace(ConfigurationRoot.EditedAuthSettings.BotAccessToken) && ConfigurationRoot.EditedAuthSettings.BotAccessToken != ConfigurationRoot.EditedAuthSettings.AccessToken; BotStatus.Text = separateBot ? "A separate bot account is authorized." : "The broadcaster account is used for chat."; }
        private void RefreshPairingStatus() { var auth = ConfigurationRoot.EditedAuthSettings; bool paired = auth.IntegrationConfigured; bool pending = !string.IsNullOrWhiteSpace(auth.IntegrationPairingRequestId) && !string.IsNullOrWhiteSpace(auth.IntegrationPairingRequestToken); RepairButton.Visibility = paired ? Visibility.Visible : Visibility.Collapsed; PairButton.Visibility = paired ? Visibility.Collapsed : Visibility.Visible; CancelPairingButton.Visibility = pending ? Visibility.Visible : Visibility.Collapsed; PairButton.Content = pending ? "Check status" : "Pair"; if (paired) SetPairingState("Paired", $"Approved for channel {auth.IntegrationChannelId}."); else if (pending) SetPairingState("Approval pending", "Press Check status to resume polling; reopening this window never resumes automatically."); else SetPairingState("Not paired", "Generate a code in Twitch Config to begin."); }
        private void ClearPendingAuthentication(bool save = true) { var auth = ConfigurationRoot.EditedAuthSettings; auth.IntegrationPairingCode = null; auth.IntegrationPairingRequestId = null; auth.IntegrationPairingRequestToken = null; auth.IntegrationCandidateCredential = null; auth.IntegrationPairingExpiresAt = null; PairingCodeTextBox.Text = string.Empty; CancelPairingButton.Visibility = Visibility.Collapsed; PairButton.Visibility = Visibility.Visible; PairButton.Content = "Pair"; if (save) ConfigurationRoot.SaveAuth(); }
        private void SetPairingState(string title, string detail) { PairingStateTitle.Text = title; PairingStateDetail.Text = detail; }
        private static string CreateFingerprint() { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName + "|BLT"))).Replace("-", string.Empty).Substring(0, 10); }
        private static string ServiceUrl => ManagedServiceDefaults.Url.TrimEnd('/');
        private sealed class PairingReceipt { public string RequestId { get; set; } public string RequestToken { get; set; } public string CandidateCredential { get; set; } public DateTimeOffset ExpiresAt { get; set; } }
        private sealed class PairingStatus { public string Status { get; set; } public string ChannelId { get; set; } public string InstallationId { get; set; } }
    }
}
