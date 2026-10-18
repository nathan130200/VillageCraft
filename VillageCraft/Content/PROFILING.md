# Performance profiling

Press **F3** to open or close the independent, movable profiler window. It is available in Debug and Release; collection is off by default. Closing the window disables collection. Capture toggles recording; Freeze retains samples while the game keeps running. Reset clears the capture at the next frame boundary. Game pause and profiler freeze are separate controls.

## Reading the numbers

- The window covers the last **240 captured frames**. Statistics refresh four times per second. Frames without a call contribute zero to frame averages and percentiles.
- Last, mean, p95 and maximum are **inclusive elapsed milliseconds per frame**, summed over calls in that frame. p95 uses the nearest-rank method. They are not individual-call percentiles.
- **Self** subtracts instrumented child scopes, including recursive/nested scopes. Sort order and the hotspot hint use mean self time. Inclusive rows overlap; do not sum them.
- **ms/call** divides total inclusive time by call count. Peak call is the slowest single invocation in the window. Calls/window counts all invocations in that window, including simulation substeps at higher speeds.
- **Self KiB/frame** measures main-thread managed allocations using `GC.GetAllocatedBytesForCurrentThread`, subtracting child allocations. Native/GPU allocations and other threads are excluded. The frame allocation summary includes all main-thread allocations inside the frame.
- GC counters are collections during captured frames since reset, across the process. They are not GC pause durations. Heap size is a periodic managed-heap estimate, not process memory usage.
- Process CPU/frame uses `Process.TotalProcessorTime` across all threads over roughly one second, divided by captured frames. It has coarser resolution than scope timers and may exceed wall time when background threads run. It is not a per-scope CPU measurement.

## Managed code, scripts and waits

Instrumentation covers frame preparation, simulation steps/day ticks, villagers, animals, combat, sunlight, roads/A*, stock and supply search, production, managed actions, world queries, callback lookup/event dispatch/queued commands, each script/callback invocation, terrain/building/resident/creature rendering, effects/picking, thumbnails, UI construction/submission, display changes, asset reads and save/load.

Jint callback inclusive time contains interpreter execution, interop and nested managed game operations. Callback self time subtracts instrumented managed operations such as A*, queries and actions, but **still includes interpreter overhead, conversions and uninstrumented CLR property access**. Query predicates running in Jint have a separate scope. This is boundary instrumentation, not a JavaScript line/function sampling profiler; it cannot attribute every CLR getter or Jint internal operation.

The activity list marks a script idle when no callback ran in the last completed frame. Failed components are also idle. Scripts execute synchronously: there is no scheduler yield, asynchronous idle duration or fabricated idle timing.

Rendering scopes measure CPU-side wall time and native submissions, **not GPU execution**. Raylib EndDrawing combines submission, buffer swap, event polling, FPS pacing and potentially driver/vsync waits. Its row is deliberately combined; high time there does not prove managed code is slow. CPU/GPU overlap cannot be separated without GPU queries or an external graphics profiler.

All scope timings use monotonic `Stopwatch.GetTimestamp`. The displayed timer tick is counter resolution, not a guarantee of measurement accuracy. OS preemption, JIT compilation and GC affect elapsed time. Scope bookkeeping and allocation counters add overhead, which remains in the measurements; no guessed overhead is subtracted. Profiler UI time is measured separately. The last collector pass is outside the frame scope and is shown separately. First-use metric allocation is included in its enclosing scope.

## Comparing captures

Warm up the scene, reset, and collect the same workload at the same camera, speed, frame cap and quality. Use Release for representative gameplay measurements. Start with self time and allocations, then inspect individual callbacks and call counts. A large inclusive callback with expensive nested A* is different from an expensive callback self time. Turn profiling off to compare its effect. Use a CLR sampling profiler or GPU profiler to investigate internals beyond these boundaries.

Export CSV writes stable scope IDs and invariant numeric values to executable-relative `UserData/Profiles/`. Exports contain the currently completed window, independent of the UI refresh interval. No per-frame disk logging occurs.

Debug-only checks:

```sh
dotnet run --project VillageCraft -c Debug -- --profile-test
dotnet run --project VillageCraft -c Debug -- --profile-benchmark
```

The benchmark is a fixed-step, flat 256x256 starter village without rendering. It reports Debug timings for diagnostics, not production performance guarantees. Structural checks cover nested accounting, script boundaries, allocations, freeze/reset, rolling-window zeros and disabled-scope allocations. Test captures belong in root `screenshots/`.
