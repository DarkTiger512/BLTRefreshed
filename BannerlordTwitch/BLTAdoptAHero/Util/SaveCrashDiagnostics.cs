using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero.Util
{
    /// <summary>
    /// Opt-in, crash-resilient save tracing and feature isolation for Bannerlord runtime diagnosis.
    /// Environment variables are intentionally used so diagnostic builds do not modify streamer YAML.
    /// </summary>
    internal static class SaveCrashDiagnostics
    {
        private const string EnabledVariable = "BLT_SAVE_DIAGNOSTICS";
        private static readonly object FileLock = new();
        private static long sequence;

        private static string LogPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Mount and Blade II Bannerlord", "logs", "BLT-save-diagnostics.log");

        internal static bool Enabled => !IsFalse(Environment.GetEnvironmentVariable(EnabledVariable));

        internal static bool GroupEnabled(string group) =>
            !IsTrue(Environment.GetEnvironmentVariable($"BLT_DISABLE_{group}")) &&
            !File.Exists(IsolationFlagPath(group));

        private static string IsolationFlagPath(string group) => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Mount and Blade II Bannerlord", "logs", $"BLT-disable-{group}.flag");

        internal static IDisposable Scope(IDataStore dataStore, string component)
        {
            if (!Enabled) return EmptyScope.Instance;
            var id = Interlocked.Increment(ref sequence);
            Write($"ENTER {id} {component} mode={Mode(dataStore)} thread={Thread.CurrentThread.ManagedThreadId}");
            return new DiagnosticScope(id, component, dataStore);
        }

        internal static void Mark(string message)
        {
            if (Enabled) Write($"MARK {message}");
        }

        private static string Mode(IDataStore dataStore) =>
            dataStore?.IsSaving == true ? "save" : dataStore?.IsLoading == true ? "load" : "other";

        private static bool IsTrue(string value) =>
            string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

        private static bool IsFalse(string value) =>
            string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "no", StringComparison.OrdinalIgnoreCase);

        private static void Write(string message)
        {
            try
            {
                lock (FileLock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                    using var stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite,
                        4096, FileOptions.WriteThrough);
                    using var writer = new StreamWriter(stream);
                    writer.WriteLine($"{DateTime.UtcNow:O} {message}");
                    writer.Flush();
                    stream.Flush(true);
                }
            }
            catch
            {
                // Diagnostics must never become another reason for the game to fail saving.
            }
        }

        private sealed class DiagnosticScope : IDisposable
        {
            private readonly long id;
            private readonly string component;
            private readonly IDataStore dataStore;
            private bool disposed;

            internal DiagnosticScope(long id, string component, IDataStore dataStore)
            {
                this.id = id;
                this.component = component;
                this.dataStore = dataStore;
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                Write($"EXIT {id} {component} mode={Mode(dataStore)} thread={Thread.CurrentThread.ManagedThreadId}");
            }
        }

        private sealed class EmptyScope : IDisposable
        {
            internal static readonly EmptyScope Instance = new();
            public void Dispose() { }
        }
    }

    /// <summary>Traces every Adopt-a-Hero campaign behavior without changing its persisted data.</summary>
    [HarmonyPatch]
    internal static class CampaignBehaviorSyncDiagnosticsPatch
    {
        private static IEnumerable<MethodBase> TargetMethods() => typeof(CampaignBehaviorSyncDiagnosticsPatch).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(CampaignBehaviorBase).IsAssignableFrom(type))
            .Select(type => type.GetMethod(nameof(CampaignBehaviorBase.SyncData),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(method => method != null);

        private static void Prefix(MethodBase __originalMethod, IDataStore __0, out IDisposable __state) =>
            __state = SaveCrashDiagnostics.Scope(__0, __originalMethod.DeclaringType?.FullName ?? "unknown behavior");

        private static void Postfix(IDisposable __state) => __state?.Dispose();

        private static Exception Finalizer(Exception __exception, IDisposable __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }
}
