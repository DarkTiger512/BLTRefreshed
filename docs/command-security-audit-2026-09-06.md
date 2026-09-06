# Viewer command audit — 6 September 2026

Audited checkout: f7971b4. Report only; no application code changed. Existing frontend modifications and deployment backups were left untouched.

This is a focused source audit of viewer command input, gold accounting, inheritance, auctions, training, betting, and HTML output. Findings below are supported by source tracing and isolated C# arithmetic checks, not live campaign reproduction. Bundled YAML establishes shipped settings, not necessarily the running streamer's settings.

## 1. P1 — Training deposits can repeatedly manufacture inheritance credit

**Commands:** `!party train <gold>`, `!party train cancel`, followed eventually by inheritance through adoption/heir.

**Evidence:** `BannerlordTwitch/BLTAdoptAHero/Actions/PartyManagement.cs:676–693` refunds cancellation with `isSpending: false`, but debits deposits with `isSpending: true`. `Behaviors/TrainingBehavior.cs:119–126` removes the fund and returns its remaining balance. `Behaviors/BLTAdoptAHeroCampaignBehavior.cs:669–687` increments `SpentGold` on spending, never reverses it on a refund, and pays inheritance on `SpentGold + Gold`.

**Reproduction in a disposable campaign:** Use an adopted hero leading a party, with training enabled and 100,000 gold. Deposit 100,000, then cancel before training consumes any funds. Repeat ten times. Wallet returns to 100,000 but `SpentGold` rises by 1,000,000. At 25% inheritance, this yields 275,000 inherited gold instead of the 25,000 baseline, excluding starting gold and prior spending. The extra 250,000 was never actually spent. A positive inheritance percentage and an eventual eligible successor are required; this is delayed gold creation, not an immediate wallet credit.

**Fix:** Treat deposits as escrow, recording inheritance-eligible spending only as training consumes funds. Alternatively reverse the refundable portion of eligible spending on cancellation using explicit accounting. Audit army/reinforcement refunds for the same pattern; passing `true` with a positive refund currently does not reverse `SpentGold` either. Do not broadly subtract all incoming gold from spending.

**Regression check:** Deposit/cancel cycles must leave both wallet and inheritance entitlement unchanged. Partially consumed training must count only the consumed portion.

## 2. P1 — Reciprocal auctions manufacture inheritance credit while preserving the gold

**Commands:** `!auction`, `!bid`, followed eventually by inheritance.

**Evidence:** `BannerlordTwitch/BLTAdoptAHero/Behaviors/BLTAdoptAHeroCampaignBehavior.cs:1171–1176` debits an auction buyer with `isSpending: true` and credits the seller the same amount. The inheritance calculation at line 683 later pays out a percentage of that buyer's recorded spending. This is a transfer between viewers, but it is counted like gold removed from the economy.

**Reproduction in a disposable campaign:** Viewer A owns a custom item and viewer B has 100,000 gold. A auctions it to B for 100,000. B then auctions it back to A for 100,000. Both wallets and item ownership return to their initial state, but A and B each gain 100,000 in `SpentGold`. Repeat auctions to accumulate entitlement; at 25% inheritance the pair gains 50,000 future gold per completed round trip. Requires two cooperating eligible heroes, enabled auctions, and positive inheritance. The bundled auction duration is 120 seconds per sale.

**Fix:** Exclude peer-to-peer auction payments from inheritance-eligible spending, or separately account for genuine gold sinks. Keep any gross-spending statistic distinct from inheritance credit. Existing inflated spending may also require a save-data decision; fixing future transactions does not remove already accumulated credit.

**Regression check:** Reciprocal sales must conserve aggregate wallet gold and must not increase aggregate inheritance entitlement.

## 3. P1 — `buyfocus` accepts a quantity that bypasses its cap and stalls command execution

**Command:** `!buyfocus <skill> <count>`.

**Evidence:** `BannerlordTwitch/BLTAdoptAHero/Actions/FocusPoints.cs:90–97` accepts any parsed `int`. Lines 135–140 cap using `focus + num > 5` and then iterate once per requested point. With current focus 1 and count 2,147,483,647, unchecked addition wraps to -2,147,483,648, so the cap is skipped and the loop attempts 2,147,483,647 iterations. The affordability check occurs after this loop. Commands run on the game thread through `BannerlordTwitch/BannerlordTwitch/Twitch/TwitchService.cs:526–549` and the integration paths at lines 567 onward.

**Impact:** A viewer with an eligible skill can trigger a prolonged game-thread stall without first owning enough gold. Negative quantities also reach `AddFocus` without validation; their precise effect depends on the game implementation. No gold-generation claim is made for this finding.

**Fix:** Reject nonpositive quantities, then cap with `Math.Min(num, 5 - focus)` after validating the existing focus range. Perform bounded cost calculation with checked or wider arithmetic.

**Regression check:** Quantities -1, 0, int.MaxValue, and an ordinary quantity must terminate promptly and never add more than the remaining capacity. Do not execute the giant loop in a live campaign.

## 4. P2 — Tournament bet accounting overflows before floating-point payout calculation

**Command:** `!bltbet`.

**Evidence:** `BannerlordTwitch/BLTAdoptAHero/Behaviors/BLTTournamentBetMissionBehavior.cs:144` accumulates a viewer's bets in an unchecked `int`. Team totals at lines 39 and 193 and payout totals at lines 230 and 240 use LINQ `Sum` over integers. Assigning the result to `double` does not widen the sum itself. For example, funded bets of 1,500,000,000 and 1,000,000,000 overflow the aggregate even though each input is individually valid.

**Impact:** Large funded pots can throw during overlay updates or settlement; repeated funded top-ups can also wrap an individual stake negative. These can disrupt payouts/refunds. This requires substantial existing funds; it is not a demonstrated way to create gold from a small balance. The post-debit update at line 154 means an update failure can occur after money has already been taken.

**Fix:** Validate accumulated bets before mutation; use checked wider storage and sums for individual stakes and the whole pot. Define wallet-cap handling for winnings and ensure errors cannot leave partially applied settlement.

**Regression check:** Multiple valid bets whose combined value exceeds int.MaxValue, funded top-ups exceeding the individual accumulator, normal payout, and refund paths.

## HTML injection and checked paths

- The recent patch changed ConsoleFeed to Vue text interpolation (`BannerlordTwitch/BannerlordTwitch/Overlay/ConsoleFeed/ConsoleFeed.html:7–11`) and structured text parts. Consequently, raw clan/kingdom names in command replies do not by themselves prove another executable HTML injection in that overlay. No matching raw-HTML sink was found in the reviewed first-party overlays or extension frontend source. Stored names remain useful validation-hardening targets, but are not presented as confirmed XSS.
- `HeroToHeroGold` rejects nonpositive amounts and insufficient funds. Shared wallet addition uses a `long` intermediate and clamps to the wallet range.
- `kingdom sponsor` already validates positive input and computes its main cost in `long`. Shipped reinforcement quantity caps keep the reviewed cost multiplication within range; extreme custom prices/caps would still warrant validation.
- Auctions recheck bidder funds and seller ownership at settlement, so merely spending a bid before auction close does not establish a gold-duplication bug.
- The reviewed chat and extension dispatch paths enforce moderator-only flags; the bundled objective administration command is moderator-only.

## Validation performed

An isolated C# probe reproduced the training arithmetic (100,000 wallet, 1,000,000 spending credit after ten refundable deposits, 275,000 inheritance at 25%), the focus addition wrap and uncapped loop bound, and LINQ integer-sum OverflowException for a 2.5-billion pot. The probe did not invoke game APIs or run the oversized loop. Auction conservation was traced through the debit, credit, and inheritance paths. Live-game timing, engine behavior after invalid focus input, deployed settings, and full campaign payout recovery remain unverified.

Suggested order: close training inheritance farming first, exclude auction transfers from inheritance credit, bound focus quantities, then widen and validate betting accounting.

## Follow-up: could pre-patch clan-name HTML injection create gold?

**Conclusion:** The pre-patch stored-HTML route is supported by source, but no complete chain from that route to an authoritative wallet increase was found. This is not a proof that every deployment or third-party plugin is safe. No live exploitation, credential access, or campaign mutation was performed.

The old `ClanManagement.HandleRenameCommand` saved the supplied name and echoed it. The old ConsoleFeed concatenated message fragments into markup and rendered them using `v-html`. Thus a name reaching the feed could alter markup and potentially execute an event handler in the overlay browser. The gold-changing operation inside rename itself is a configured debit, not interpretation of the name as a numeric amount or command.

| Candidate route | Assessment and missing link |
| --- | --- |
| Fake gold/balance/reward messages | Feasible display manipulation in the vulnerable overlay. It changes browser output, not `HeroData.Gold`; no reviewed path consumes the manipulated DOM as wallet state. A human might be deceived into issuing a manual grant, but that is not an automatic mint. |
| Invoke a local SignalR gold/admin method | No matching method found. All six repository Hub classes were inventoried. Their exposed application methods refresh display data; `ChangeHeroGold`, `TestCommand`, and reward handlers are not Hub methods. Public static broadcast helpers are not equivalent to browser-callable RPC endpoints. |
| Replay overlay messages to repeat a reward | ConsoleFeed `Refresh` replays up to 100 stored display messages to its caller. It does not rerun the originating command or reward. Replay can retrigger old injected browser content, but not the associated game transaction. |
| Call the extension command/action API as broadcaster | Endpoints require a validated bearer identity and build the command user from that identity. Overlay scripts do not receive that bearer credential. The game also enforces moderator-only command settings. An additional credential exposure or authentication bypass is required. |
| Steal an authenticated Twitch/extension browser session | The overlay runs under its own origin and is not automatically the broadcaster's logged-in Twitch page. No privileged token was found in overlay source or Hub responses. Cross-origin storage is separated by the browser's same-origin policy. A separately exposed token or browser compromise would change this assessment. |
| Read local auth/config files through the overlay server | The configured static root is the module's `web` directory; auth is loaded from the game's configuration directory. The custom filesystem rejects lexical paths outside its root. It resolves symbolic links after that check, so a deliberately/mistakenly exposed link could weaken confinement; no such web-root link is established by the reviewed packaging code. This is conditional file exposure, not a demonstrated token-to-gold chain. The additional default `UseStaticFiles()` middleware also warrants deployed-content inspection because its default root is not set explicitly here. |
| Impersonate the game WebSocket | `/ws/game/{channel}` validates an installation credential. Overlay code does not receive it. Sending invented game-state JSON locally is not an authoritative game-wallet mutation. |
| Abuse development authentication | A real conditional risk exists: the backend contains a fixed development bearer bypass, gated on Development mode AND `BLT_ALLOW_DEVELOPMENT_AUTH=true`, with a configurable role defaulting to broadcaster. If such a backend serves a connected game and is reachable, privileged commands may become available without normal Twitch authentication. This does not require HTML injection, and no deployed enablement was verified. |
| Reach another localhost service or OBS integration | Conditional on installed software exposing a reachable interface that can control game commands/files, and on its authentication and browser restrictions. No such bridge was found in this repository. The BLT overlay's permissive CORS is not itself a gold-writing capability. |
| Turn the saved clan name into income or a game command | Reviewed income code uses clan/hero objects and numeric settings for payouts; names are display text. No evaluated-JavaScript, shell execution, or wallet amount derived from the renamed string was found. Bot replies add their normal reply prefix; no direct reply-to-privileged-command route was established. |
| Automate the training/auction inheritance exploits | These are real command-accounting issues described above. An authenticated viewer can use them without HTML injection. The overlay injection alone does not provide the viewer authentication required to call extension APIs. |

Key inspected files: `BannerlordTwitch/BannerlordTwitch/Overlay/BLTOverlay.cs:80–95`, `Overlay/TwitchHub.cs:28`, `Overlay/ConsoleFeed/ConsoleFeed.cs:32`, `Overlay/PhysicalFileSystemEx.cs:74–82`, `Settings/AuthSettings.cs:45–55`, `Twitch/TwitchService.cs:526–632`, `TwitchExtension/backend/BLT.ExtensionService/Program.cs:141–185,214–229`, and `Security/TwitchExtensionTokenValidator.cs:12–48`.

Browser-origin reference: [MDN same-origin policy](https://developer.mozilla.org/en-US/docs/Web/Security/Defenses/Same-origin_policy). Hub API reference: [Microsoft ASP.NET SignalR server guide](https://learn.microsoft.com/en-us/aspnet/signalr/overview/guide-to-the-api/hubs-api-guide-server).

For this particular injection, preserve the shared text-rendering fix and add consistent validation to saved names as defense in depth. Previously saved malicious names or a still-running pre-patch overlay require attention: a server patch does not retroactively undo JavaScript already executed in an existing browser document. Real wallet changes should be assessed from authoritative balances/save data, not overlay screenshots.
