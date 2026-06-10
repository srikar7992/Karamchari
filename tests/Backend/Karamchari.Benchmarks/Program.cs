// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using BenchmarkDotNet.Running;

BenchmarkSwitcher
    .FromAssembly(typeof(Karamchari.Benchmarks.BurnoutScoreCalculatorBenchmarks).Assembly)
    .Run(args);
