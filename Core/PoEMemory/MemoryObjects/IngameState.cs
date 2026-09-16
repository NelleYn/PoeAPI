using System;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects
{
    public class IngameState : RemoteMemoryObject
    {
        private readonly CachedValue<Camera> _camera;
        private readonly CachedValue<float> _CurrentUElementPosX;
        private readonly CachedValue<float> _CurrentUElementPosY;
        private readonly CachedValue<DiagnosticInfoType> _DiagnosticInfoType;
        private readonly CachedValue<EntityLabelMapOffsets> _EntityLabelMap;
        private readonly CachedValue<DiagnosticElement> _FPSRectangle;
        private readonly CachedValue<DiagnosticElement> _FrameTimeRectangle;
        private readonly CachedValue<IngameData> _ingameData;
        private readonly CachedValue<IngameStateOffsets> _ingameState;
        private readonly CachedValue<IngameUIElements> _ingameUi;
        private readonly CachedValue<DiagnosticElement> _LatencyRectangle;
        private CachedValue<Element> _mouseActions;
        private readonly CachedValue<ServerData> _serverData;
        private readonly CachedValue<float> _TimeInGameF;
        private readonly CachedValue<Element> _UIHover;
        private readonly CachedValue<Element> _UIHoverTooltip;
        private readonly CachedValue<float> _UIHoverX;
        private readonly CachedValue<float> _UIHoverY;
        private readonly CachedValue<Element> _UIRoot;

        /// <summary>
        /// Lowest latency, in milliseconds, that <see cref="CurLatency"/> will accept as a real
        /// measurement. Zero is what an unpopulated or wrongly addressed field reads as, and a
        /// negative number cannot be a round trip, so both are rejected rather than propagated.
        /// </summary>
        private const float MinPlausibleLatency = 1f;

        /// <summary>
        /// Highest latency, in milliseconds, that <see cref="CurLatency"/> will accept. Chosen at
        /// one second: past that, pacing the engine's caches by latency costs more than it saves —
        /// every latency-paced cache would go a full second between memory reads and the overlay
        /// would be drawing second-old state — and a number that large is far more likely to be a
        /// misread field than a connection anyone is playing on.
        /// </summary>
        private const float MaxPlausibleLatency = 1000f;

        /// <summary>
        /// Value <see cref="CurLatency"/> reports when the reading is missing or implausible. It is
        /// deliberately the same 25 ms that <see cref="CachedValue.Latency"/> is initialised to, so
        /// that a rejected reading leaves the engine pacing itself exactly as it does before the
        /// first successful one.
        /// </summary>
        private const float DefaultLatency = 25f;

        /// <summary>
        /// How long <see cref="CurLatency"/> waits before repeating a complaint about the same
        /// implausible reading. The property is read once per engine tick, so an unconditional log
        /// line would be its own damage; a rejection that persists is still worth restating, because
        /// the engine stays off its measured pacing for as long as it lasts.
        /// </summary>
        private static readonly TimeSpan ImplausibleLatencyRepeatInterval = TimeSpan.FromSeconds(30);

        /// <summary>True while the last reading examined by <see cref="CurLatency"/> was rejected.</summary>
        private bool _latencyImplausible;

        /// <summary>When the last rejection was written to the log.</summary>
        private DateTime _lastBadLatencyReportUtc = DateTime.MinValue;

        public IngameState()
        {
            _ingameState = new FrameCache<IngameStateOffsets>(() => M.Read<IngameStateOffsets>(Address /*+M.offsets.IgsOffsetDelta*/));

            // Camera is reached through a POINTER, not as a struct embedded in IngameState. The
            // address the reference reports for it is lower than IngameState's own, so the old
            // "Address + offset" model could not reach it at any offset; what it did reach, at
            // 0xF4C, was a text buffer.
            _camera = new AreaCache<Camera>(() => GetObject<Camera>(_ingameState.Value.Camera));

            _ingameData = new AreaCache<IngameData>(() => GetObject<IngameData>(_ingameState.Value.Data));
            // ServerData is NOT a field of IngameState on this client. Measured: the true address
            // the reference distribution reports for it does not occur ANYWHERE in the first 0x2000
            // bytes of IngameState, while it sits at IngameData + 0x968 as the only match in the
            // window, in two different zones. So it is reached through Data, exactly as the
            // reference reaches it; IngameStateOffsets.ServerData is an older build's number and is
            // no longer read. This is also why InGame used to report false while the game state
            // controller said we were in game: the flag was being read out of an unrelated address.
            _serverData = new AreaCache<ServerData>(() => GetObject<ServerData>(Data?.DataStruct.ServerData ?? 0));
            _ingameUi = new AreaCache<IngameUIElements>(() => GetObject<IngameUIElements>(_ingameState.Value.IngameUi));
            _UIRoot = new AreaCache<Element>(() => GetObject<Element>(_ingameState.Value.UIRoot));
            _UIHover = new FrameCache<Element>(() => GetObject<Element>(_ingameState.Value.UIHover));
            _UIHoverX = new FrameCache<float>(() => _ingameState.Value.UIHoverX);
            _UIHoverY = new FrameCache<float>(() => _ingameState.Value.UIHoverY);
            _UIHoverTooltip = new FrameCache<Element>(() => GetObject<Element>(_ingameState.Value.UIHoverTooltip));
            _CurrentUElementPosX = new FrameCache<float>(() => _ingameState.Value.CurentUElementPosX);
            _CurrentUElementPosY = new FrameCache<float>(() => _ingameState.Value.CurentUElementPosY);
            _DiagnosticInfoType = new FrameCache<DiagnosticInfoType>(() => (DiagnosticInfoType) _ingameState.Value.DiagnosticInfoType);

            _LatencyRectangle = new AreaCache<DiagnosticElement>(
                () => GetObject<DiagnosticElement>(
                    Address + Extensions.GetOffset<IngameStateOffsets>(nameof(IngameStateOffsets.LatencyRectangle))));

            _FrameTimeRectangle = new AreaCache<DiagnosticElement>(
                () => GetObject<DiagnosticElement>(Address + /*0x1628*/
                                                   +Extensions.GetOffset<IngameStateOffsets>(
                                                       nameof(IngameStateOffsets.FrameTimeRectangle))));

            _FPSRectangle = new AreaCache<DiagnosticElement>(
                () => GetObject<DiagnosticElement>(Address /*0x1870*/ +
                                                   Extensions.GetOffset<IngameStateOffsets>(nameof(IngameStateOffsets.FPSRectangle))));

            _TimeInGameF = new FrameCache<float>(() => _ingameState.Value.TimeInGameF);
            _EntityLabelMap = new AreaCache<EntityLabelMapOffsets>(() => M.Read<EntityLabelMapOffsets>(_ingameState.Value.EntityLabelMap));
        }

        public Camera Camera => _camera.Value;
        public IngameData Data => _ingameData.Value;
        public bool InGame => ServerData.IsInGame;
        public ServerData ServerData => _serverData.Value;
        public IngameUIElements IngameUi => _ingameUi.Value;
        public Element UIRoot => _UIRoot.Value;
        public Element UIHover => _UIHover.Value;
        public Element UIHoverElement => UIHover;
        public float UIHoverX => _UIHoverX.Value;
        public float UIHoverY => _UIHoverY.Value;
        public Element UIHoverTooltip => _UIHoverTooltip.Value;
        public float CurentUElementPosX => _CurrentUElementPosX.Value;
        public float CurentUElementPosY => _CurrentUElementPosY.Value;
        public long EntityLabelMap => _EntityLabelMap.Value.EntityLabelMap;
        public DiagnosticInfoType DiagnosticInfoType => _DiagnosticInfoType.Value;
        public DiagnosticElement LatencyRectangle => _LatencyRectangle.Value;
        public DiagnosticElement FrameTimeRectangle => _FrameTimeRectangle.Value;
        public DiagnosticElement FPSRectangle => _FPSRectangle.Value;

        /// <summary>
        /// Round-trip latency to the game server, in milliseconds, or <see cref="DefaultLatency"/>
        /// when no plausible reading is available.
        /// </summary>
        /// <remarks>
        /// <para>
        /// SOURCE. Read from <see cref="ExileCore.PoEMemory.MemoryObjects.ServerData.Latency"/>, not from
        /// <see cref="LatencyRectangle"/>. The rectangle is a HUD diagnostic element: its CurrValue
        /// only carries a number while the client is actually drawing the latency readout, and with
        /// the diagnostic overlay off it reads 0. That 0 was not cosmetic. GameController.Tick
        /// assigns this property to <see cref="CachedValue.Latency"/>, and LatancyCache uses that
        /// as the minimum interval between refreshes; a 0 falls below every cache's own floor, so
        /// the whole engine silently dropped to the 10 ms default floor instead of the 25 ms it is
        /// initialised with — roughly 2.5x the memory traffic, with no message anywhere.
        /// </para>
        /// <para>
        /// THE OFFSET IT READS. <see cref="ServerDataOffsets.LATENCY"/>, measured at
        /// ServerData.Address + 0xC490 on the live client on 2026-09-16: the low uint32 there read
        /// 3 at a moment when the reference distribution also reported Latency = 3. The offset it
        /// replaced, 0x6CA0, read 0x128CBA26 at the same moment, which is not a latency under any
        /// interpretation. Stated honestly, that agreement is by itself weak - 3 is a small number
        /// and will coincide with many unrelated fields. Its force comes from the pair around it:
        /// the offset was found by an independent survey rather than by scanning for a value that
        /// matched, and the number it displaces reads demonstrable nonsense. The full provenance,
        /// with the same caveat spelled out, is on ServerDataOffsets.LATENCY.
        /// </para>
        /// <para>
        /// WHY IT CLAMPS INSTEAD OF PASSING THE NUMBER THROUGH. Memory.Read returns default on a
        /// bad address rather than throwing, so a stale offset yields either 0 or an arbitrary
        /// number, and both would be accepted as a cache period without a word. Anything outside
        /// [<see cref="MinPlausibleLatency"/>, <see cref="MaxPlausibleLatency"/>] is therefore
        /// replaced by <see cref="DefaultLatency"/>, which keeps the engine at its documented
        /// default pacing rather than collapsing to the 10 ms floor or stalling on a five-figure
        /// period.
        /// </para>
        /// <para>
        /// AND WHY THE CLAMP IS NOT SILENT. Falling back changes how often the WHOLE engine re-reads
        /// game memory, and doing that without saying so is how the LatencyRectangle bug went
        /// unnoticed in the first place: everything kept working, just at 2.5x the memory traffic,
        /// with nothing anywhere to read. So a rejected reading is logged - once when the rejection
        /// starts, and after that no more often than <see cref="ImplausibleLatencyRepeatInterval"/>,
        /// since this property is read every tick and a per-tick log line would be its own kind of
        /// damage. The interval is NOT reset by the rejected value changing: a stale offset reads
        /// fresh garbage every frame, so that would have uncapped the log exactly when it was most
        /// wrong. Recovery is logged too:
        /// "it stopped complaining" must not be indistinguishable from "it is still broken and gave
        /// up". Out of game is NOT reported - an unanchored ServerData is an expected state, not a
        /// misread offset.
        /// </para>
        /// </remarks>
        public float CurLatency
        {
            get
            {
                var serverData = ServerData;

                // ServerData is materialized unconditionally by GetObject, so it is never null but
                // may be anchored at 0 while out of game. There is no reading to judge then, and
                // nothing to complain about either: leave the reporting state untouched so that
                // walking through a loading screen does not manufacture a "recovered" line.
                if (serverData == null || serverData.Address == 0)
                    return DefaultLatency;

                float latency = serverData.Latency;

                if (latency >= MinPlausibleLatency && latency <= MaxPlausibleLatency)
                {
                    NoteLatencyAccepted(latency);
                    return latency;
                }

                NoteLatencyRejected(latency, serverData.Address);
                return DefaultLatency;
            }
        }

        /// <summary>
        /// Records a reading that passed the plausibility range, and says so in the log if the
        /// previous one had failed it.
        /// </summary>
        private void NoteLatencyAccepted(float latency)
        {
            if (!_latencyImplausible)
                return;

            _latencyImplausible = false;
            _lastBadLatencyReportUtc = DateTime.MinValue;

            ReportLatencyState($"{nameof(IngameState)}.{nameof(CurLatency)}: latency reads {latency:0.##} ms again, " +
                   $"back inside [{MinPlausibleLatency:0.##}, {MaxPlausibleLatency:0.##}] ms; " +
                   "cache pacing follows the measured value once more.");
        }

        /// <summary>
        /// Records a reading that failed the plausibility range and reports it, rate-limited so that
        /// a per-tick read cannot turn into a per-tick log line.
        /// </summary>
        /// <param name="latency">The reading that was rejected.</param>
        /// <param name="serverDataAddress">
        /// Where it was read from, so the line names an address that can be checked by hand rather
        /// than only an implausible number.
        /// </param>
        private void NoteLatencyRejected(float latency, long serverDataAddress)
        {
            var now = DateTime.UtcNow;

            // The rate limit is on TIME ALONE, deliberately: only the transition from accepted to
            // rejected earns an immediate line, and after that the interval governs no matter what
            // the number does. An earlier version also let a CHANGED value through, which sounds
            // like useful detail and is the opposite: the worst case this log exists for is a stale
            // offset, and a stale offset reads a different piece of unrelated memory every frame, so
            // "the value changed" was true on every tick precisely when the complaint was loudest.
            // The per-tick flood the comment promises to avoid was therefore guaranteed in the one
            // scenario that matters. The value still reaches the reader - it is printed in the line
            // that does get through.
            var justWentImplausible = !_latencyImplausible;

            _latencyImplausible = true;

            if (!justWentImplausible && now - _lastBadLatencyReportUtc < ImplausibleLatencyRepeatInterval)
                return;

            _lastBadLatencyReportUtc = now;

            ReportLatencyState($"{nameof(IngameState)}.{nameof(CurLatency)}: implausible latency {latency:0.##} ms from " +
                   $"ServerData 0x{serverDataAddress:X}+0x{ServerDataOffsets.LATENCY:X} " +
                   $"(accepted range [{MinPlausibleLatency:0.##}, {MaxPlausibleLatency:0.##}] ms). " +
                   $"Falling back to {DefaultLatency:0.##} ms, which paces EVERY latency-driven cache " +
                   "in the engine - so this is a wrong offset or a wrong ServerData anchor, not a cosmetic " +
                   $"miss. Repeats at most every {ImplausibleLatencyRepeatInterval.TotalSeconds:0} s.");
        }

        /// <summary>
        /// Writes a diagnostic line without letting the diagnostic itself break the caller.
        /// </summary>
        /// <remarks>
        /// <see cref="DebugWindow"/> is static engine state that may not be standing yet (or may be
        /// coming down) while memory objects are still being read. A latency report is worth strictly
        /// less than the frame it would take down, so it is swallowed rather than propagated.
        /// </remarks>
        private static void ReportLatencyState(string message)
        {
            try { DebugWindow.LogError(message, 5f); }
            catch { /* nothing useful to add: the thing that reports problems is itself the problem */ }
        }

        public float CurFrameTime => FrameTimeRectangle.CurrValue;
        public float CurFps => FPSRectangle.CurrValue;
        public TimeSpan TimeInGame => TimeSpan.FromSeconds(_ingameState.Value.TimeInGame);
        public float TimeInGameF => _TimeInGameF.Value;

        public void UpdateData()
        {
            _ingameData.ForceUpdate();
        }
    }
}
