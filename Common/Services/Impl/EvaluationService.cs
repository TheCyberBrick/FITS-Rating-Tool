/*
    FITS Rating Tool
    Copyright (C) 2022 TheCyberBrick
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using FitsRatingTool.Common.Models.Evaluation;
using FitsRatingTool.Common.Models.FitsImage;
using FitsRatingTool.Common.Utils;
using Microsoft.VisualStudio.Threading;
using org.matheval;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using static FitsRatingTool.Common.Models.FitsImage.IFitsImageStatistics;

namespace FitsRatingTool.Common.Services.Impl
{
    public class EvaluationService : IEvaluationService
    {
        private class Evaluator : IEvaluationService.IEvaluator
        {
            public string Formula { get; }

            public bool IsThreadSafe => false;


            private readonly EvaluationService evaluator;
            private readonly List<Variable> variables;

            public Evaluator(EvaluationService evaluator, List<Variable> variables, string formula)
            {
                Formula = formula;
                this.evaluator = evaluator;
                this.variables = variables;
            }

            public IEvaluationService.IEvaluator Clone()
            {
                List<Variable> clonedVariables = new();

                foreach (var variable in variables)
                {
                    var clonedVariable = new Variable(variable.Name, variable.Position);
                    clonedVariable.Formula.Append(variable.Formula);
                    clonedVariable.Expression = new Expression(clonedVariable.Formula.ToString());
                    clonedVariables.Add(clonedVariable);
                }

                return new Evaluator(evaluator, clonedVariables, Formula);
            }

            private async Task EvaluateAsync(
                int n,
                int startVarIndex, int endVarIndex,
                bool statisticsRequired,
                IFitsImageStatistics stats,
                Dictionary<string, StatsInfo> additionalStatistics,
                Dictionary<string, StatsInfo> additionalVariablesStatistics,
                Dictionary<string, double> evaluatedVariables,
                IEvaluationService.IEvaluator.EvaluationConsumer consumer,
                List<double> intermediateResults,
                IEvaluationService.IEvaluator.EventConsumer? eventConsumer = default,
                CancellationToken cancellationToken = default)
            {
                for (int i = startVarIndex; i <= endVarIndex; ++i)
                {
                    eventConsumer?.Invoke(new Evaluation.EvaluationStepEvent(Evaluation.Phase.EvaluationStepStart, i, n, stats));

                    cancellationToken.ThrowIfCancellationRequested();

                    var variable = variables[i];

                    if (variable.Expression == null)
                    {
                        throw new InvalidOperationException("Variable expression is null");
                    }

                    SetStatsBindings(variable.Expression, stats, additionalStatistics);

                    foreach (var v in evaluatedVariables)
                    {
                        variable.Expression.Bind(v.Key, v.Value);
                    }

                    SetAdditionalVariablesBindings(variable.Expression, evaluatedVariables, additionalVariablesStatistics);

                    double result;

                    try
                    {
                        result = Convert.ToDouble(variable.Expression.Eval());
                    }
                    catch (DivideByZeroException)
                    {
                        result = double.NaN;
                    }

                    evaluatedVariables[variable.Name] = result;

                    if (i == n - 1)
                    {
                        IEnumerable<KeyValuePair<string, double>> variableValuesEnumerable()
                        {
                            foreach (var measurementClass in MeasurementClasses)
                            {
                                if (stats.GetValue(measurementClass.Value, out var val))
                                {
                                    yield return KeyValuePair.Create(measurementClass.Name, val);
                                }
                            }

                            foreach (var entry in additionalStatistics)
                            {
                                var name = entry.Key;
                                var value = entry.Value;

                                double sigma = value.mad > 0 && stats.GetValue(value.measurement, out var svalue) ? (svalue - value.median) / value.mad : 0;
                                yield return KeyValuePair.Create(name + "Sigma", sigma);

                                yield return KeyValuePair.Create(name + "Min", value.min);
                                yield return KeyValuePair.Create(name + "Max", value.max);
                                yield return KeyValuePair.Create(name + "Median", value.median);
                            }

                            foreach (var entry in evaluatedVariables)
                            {
                                yield return KeyValuePair.Create(entry.Key, entry.Value);
                            }

                            foreach (var entry in additionalVariablesStatistics)
                            {
                                var name = entry.Key;
                                var value = entry.Value;

                                double sigma = value.mad > 0 ? (evaluatedVariables[name] - value.median) / value.mad : 0;
                                yield return KeyValuePair.Create(name + "Sigma", sigma);

                                yield return KeyValuePair.Create(name + "Min", value.min);
                                yield return KeyValuePair.Create(name + "Max", value.max);
                                yield return KeyValuePair.Create(name + "Median", value.median);
                            }
                        }
                        await consumer.Invoke(stats, variableValuesEnumerable(), result, cancellationToken);
                    }
                    else if (i == endVarIndex && statisticsRequired)
                    {
                        lock (intermediateResults)
                        {
                            intermediateResults.Add(result);
                        }
                    }

                    eventConsumer?.Invoke(new Evaluation.EvaluationStepEvent(Evaluation.Phase.EvaluationStepEnd, i, n, stats));
                }
            }

            private async Task<Dictionary<string, StatsInfo>> PrecomputeAdditionalStatisticsAsync(IEnumerable<IFitsImageStatistics> statistics, IEvaluationService.IEvaluator.EventConsumer? eventConsumer = default, CancellationToken cancellationToken = default)
            {
                HashSet<Task<Tuple<string, StatsInfo>>> tasks = new();

                void checkAndEnqueue(MeasurementType measurement, string name, HashSet<string> usedVariables, HashSet<string> precomputedVariables)
                {
                    if ((usedVariables.Contains(name + "Sigma") || usedVariables.Contains(name + "Min") || usedVariables.Contains(name + "Max") || usedVariables.Contains(name + "Median")) && precomputedVariables.Add(name))
                    {
                        eventConsumer?.Invoke(new Evaluation.StatisticsEvent(Evaluation.Phase.PrecomputeStatisticsStart, name));

                        tasks.Add(Task.Run(() =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            CalculateStatistics(measurement, statistics, out var min, out var max, out var median, out var mad, cancellationToken);
                            return Tuple.Create(name, new StatsInfo(measurement, min, max, median, mad));
                        }));
                    }
                }

                HashSet<string> precomputedVariables = new();

                foreach (var variable in variables)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var expression = variable.Expression;

                    if (expression != null)
                    {
                        HashSet<string> usedVariables;
                        try
                        {
                            usedVariables = new HashSet<string>(expression.getVariables());
                        }
                        catch (Exception)
                        {
                            usedVariables = new HashSet<string>();
                        }

                        foreach (var measurementClass in MeasurementClasses)
                        {
                            checkAndEnqueue(measurementClass.Value, measurementClass.Name, usedVariables, precomputedVariables);

                            foreach (var measurementName in measurementClass.Measurements.Keys)
                            {
                                if (!measurementName.Equals(measurementClass.Name))
                                {
                                    checkAndEnqueue(measurementClass.Measurements[measurementName], measurementName, usedVariables, precomputedVariables);
                                }
                            }
                        }
                    }
                }

                Dictionary<string, StatsInfo> additionalStatistics = new();

                while (tasks.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var task = await Task.WhenAny(tasks);
                    tasks.Remove(task);

                    var tuple = await task;

                    additionalStatistics.Add(tuple.Item1, tuple.Item2);

                    eventConsumer?.Invoke(new Evaluation.StatisticsEvent(Evaluation.Phase.PrecomputeStatisticsEnd, tuple.Item1));
                }

                return additionalStatistics;
            }

            public async Task EvaluateAsync(IEnumerable<IFitsImageStatistics> statistics, int parallelTasks,
                IEvaluationService.IEvaluator.EvaluationConsumer consumer, IEvaluationService.IEvaluator.EventConsumer? eventConsumer = default,
                CancellationToken cancellationToken = default)
            {
                int n = variables.Count;

                int startVarIndex = 0;
                int endVarIndex = 0;

                Dictionary<string, StatsInfo> additionalStatistics = await PrecomputeAdditionalStatisticsAsync(statistics, eventConsumer, cancellationToken);

                // Additional variable statistics are computed on the fly because they might
                // depend on other variables
                Dictionary<string, StatsInfo> additionalVariablesStatistics = new();

                Dictionary<IFitsImageStatistics, Dictionary<string, double>> evaluatedVariablesMap = new();
                foreach (var stats in statistics)
                {
                    evaluatedVariablesMap.Add(stats, new());
                }

                List<Evaluator> evaluatorPool = new();
                for (int i = 0; i < parallelTasks; i++)
                {
                    evaluatorPool.Add((Evaluator)Clone());
                }

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    endVarIndex = startVarIndex;

                    bool statisticsRequired = false;

                    // Find range up to and including next variable for which
                    // statistics need to be calculated. After that variable
                    // all threads/tasks need to synchronize so that the
                    // statistics can be calculated.
                    for (int i = startVarIndex; i < n; ++i)
                    {
                        var variable = variables[i];

                        if (variable.StatisticsRequired)
                        {
                            statisticsRequired = true;
                            break;
                        }

                        endVarIndex = Math.Min(endVarIndex + 1, n - 1);
                    }

                    if (startVarIndex >= variables.Count)
                    {
                        // All variables evaluated
                        return;
                    }

                    List<double> intermediateResults = new();

                    // Evaluate statistics in parallel
                    IEnumerable<Func<Evaluator, Func<CancellationToken, Task>>> taskGenerator()
                    {
                        foreach (var stats in statistics)
                        {
                            yield return instance => ct => Task.Run(async () => await instance.EvaluateAsync(
                                n, startVarIndex, endVarIndex,
                                statisticsRequired, stats, additionalStatistics, additionalVariablesStatistics,
                                evaluatedVariablesMap[stats], consumer, intermediateResults, eventConsumer, ct));
                        }
                    }
                    await PooledResourceTaskRunner.RunAsync(evaluatorPool, taskGenerator(), cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    startVarIndex = endVarIndex + 1;

                    // Calculate and store statistics of variable
                    if (statisticsRequired)
                    {
                        double min = intermediateResults.Min();
                        double max = intermediateResults.Max();
                        CalculateStatistics(intermediateResults, min, max, out var median, out var mad, cancellationToken);
                        additionalVariablesStatistics[variables[endVarIndex].Name] = new StatsInfo(null, min, max, median, mad);
                    }
                }
            }

        }

        private struct StatsInfo
        {
            public readonly MeasurementType? measurement;
            public readonly double min, max, median, mad;

            public StatsInfo(MeasurementType? measurement, double min, double max, double median, double mad)
            {
                this.measurement = measurement;
                this.min = min;
                this.max = max;
                this.median = median;
                this.mad = mad;
            }
        }


        private class MeasurementClass
        {
            public readonly string Name;
            public readonly MeasurementType Value;
            public readonly MeasurementType? Max;
            public readonly MeasurementType? Min;
            public readonly MeasurementType? Mean;
            public readonly MeasurementType? Median;
            public readonly MeasurementType? MeanDev;

            public readonly IReadOnlyDictionary<string, MeasurementType> Measurements;

            public MeasurementClass(
                string name,
                MeasurementType value,
                MeasurementType? max,
                MeasurementType? min,
                MeasurementType? mean,
                MeasurementType? median,
                MeasurementType? meanDev
                )
            {
                Name = name;
                Value = value;
                Max = max;
                Min = min;
                Mean = mean;
                Median = median;
                MeanDev = meanDev;

                var dict = new Dictionary<string, MeasurementType>();
                dict.TryAdd(Name, Value);
                if (Max.HasValue) dict.TryAdd(Name + "SubMax", Max.Value);
                if (Min.HasValue) dict.TryAdd(Name + "SubMin", Min.Value);
                if (Mean.HasValue) dict.TryAdd(Name + "SubMean", Mean.Value);
                if (Median.HasValue) dict.TryAdd(Name + "SubMedian", Median.Value);
                if (MeanDev.HasValue)
                {
                    dict.TryAdd(Name + "SubMeanDev", MeanDev.Value);
                    dict.TryAdd(Name + "MeanDev", MeanDev.Value);
                }
                Measurements = dict;
            }
        }

        private static readonly List<MeasurementClass> MeasurementClasses = new()
        {
            new MeasurementClass("Stars", MeasurementType.Stars, null, null, null, null, null),
            new MeasurementClass("Median", MeasurementType.Median, null, null, null, null, MeasurementType.MedianMAD),
            new MeasurementClass("Noise", MeasurementType.Noise, null, null, null, null, null),
            new MeasurementClass("NoiseRatio", MeasurementType.NoiseRatio, null, null, null, null, null),
            new MeasurementClass("Eccentricity", MeasurementType.EccentricityMean, MeasurementType.EccentricityMax, MeasurementType.EccentricityMin, MeasurementType.EccentricityMean, MeasurementType.EccentricityMedian, MeasurementType.EccentricityMAD),
            new MeasurementClass("SNR", MeasurementType.SNRMean, MeasurementType.SNRMax, MeasurementType.SNRMin, MeasurementType.SNRMean, MeasurementType.SNRMedian, MeasurementType.SNRMAD),
            new MeasurementClass("SNRWeight", MeasurementType.SNRWeight, null, null, null, null, null),
            new MeasurementClass("FWHM", MeasurementType.FWHMMean, MeasurementType.FWHMMax, MeasurementType.FWHMMin, MeasurementType.FWHMMean, MeasurementType.FWHMMedian, MeasurementType.FWHMMAD),
            new MeasurementClass("HFR", MeasurementType.HFRMean, MeasurementType.HFRMax, MeasurementType.HFRMin, MeasurementType.HFRMean, MeasurementType.HFRMedian, MeasurementType.HFRMAD),
            new MeasurementClass("HFD", MeasurementType.HFDMean, MeasurementType.HFDMax, MeasurementType.HFDMin, MeasurementType.HFDMean, MeasurementType.HFDMedian, MeasurementType.HFDMAD),
            new MeasurementClass("Residual", MeasurementType.ResidualMean, MeasurementType.ResidualMax, MeasurementType.ResidualMin, MeasurementType.ResidualMean, MeasurementType.ResidualMedian, MeasurementType.ResidualMAD)
        };

        private static void CalculateStatistics(List<double> values, double min, double max, out double median, out double mad, CancellationToken cancellationToken = default)
        {
            int i = values.Count;

            median = 0;
            mad = 0;

            if (i > 0)
            {
                values.Sort();

                if (i <= 1)
                {
                    median = values[0];
                }
                else if (i % 2 == 0)
                {
                    int half = i / 2 - 1;
                    median = (values[half] + values[half + 1]) * 0.5;
                }
                else
                {
                    median = values[(i - 1) / 2];
                }

                int j = 0;
                foreach (var value in values)
                {
                    mad += Math.Abs(median - value);

                    if (++j % 10000 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                mad /= i;
            }
        }

        private static void CalculateStatistics(MeasurementType measurement, IEnumerable<IFitsImageStatistics> statistics, out double min, out double max, out double median, out double mad, CancellationToken cancellationToken = default)
        {
            int i = 0;
            min = double.MaxValue;
            max = double.MinValue;

            var values = new List<double>();

            int j = 0;
            foreach (var fstats in statistics)
            {
                if (fstats.GetValue(measurement, out var fvalue))
                {
                    ++i;
                    values.Add(fvalue);
                    min = Math.Min(min, fvalue);
                    max = Math.Max(max, fvalue);
                }

                if (++j % 10000 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            CalculateStatistics(values, min, max, out median, out mad, cancellationToken);
        }

        private static void SetAdditionalStatsBindings(Expression expression, MeasurementType measurement, string name, IFitsImageStatistics stats, IDictionary<string, StatsInfo> additionalStatisticsCache)
        {
            if (additionalStatisticsCache.TryGetValue(name, out var tuple))
            {
                double sigma = tuple.mad > 0 && stats.GetValue(measurement, out var svalue) ? (svalue - tuple.median) / tuple.mad : 0;
                expression.Bind(name + "Sigma", sigma);

                expression.Bind(name + "Min", tuple.min);
                expression.Bind(name + "Max", tuple.max);
                expression.Bind(name + "Median", tuple.median);
            }
        }

        private static void SetAdditionalStatsBindingsCheck(Expression expression, string name, Random rng)
        {
            expression.Bind(name + "Sigma", 1.123456 + rng.NextDouble());
            expression.Bind(name + "Min", 1.123456 + rng.NextDouble());
            expression.Bind(name + "Max", 1.123456 + rng.NextDouble());
            expression.Bind(name + "Median", 1.123456 + rng.NextDouble());
        }

        private static void SetStatsBindings(Expression expression, IFitsImageStatistics stats, IDictionary<string, StatsInfo> additionalStatistics)
        {
            foreach (var measurementClass in MeasurementClasses)
            {
                SetAdditionalStatsBindings(expression, measurementClass.Value, measurementClass.Name, stats, additionalStatistics);

                foreach (var measurementName in measurementClass.Measurements.Keys)
                {
                    if (!measurementName.Equals(measurementClass.Name))
                    {
                        SetAdditionalStatsBindings(expression, measurementClass.Measurements[measurementName], measurementName, stats, additionalStatistics);
                    }
                }
            }

            foreach (var measurementClass in MeasurementClasses)
            {
                foreach (var measurementName in measurementClass.Measurements.Keys)
                {
                    var measurement = measurementClass.Measurements[measurementName];

                    expression.Bind(measurementName, stats.GetValue(measurement, out var value) ? value : 0.0);
                }
            }
        }

        private static void SetStatsBindingsCheck(Expression expression, Random rng)
        {
            foreach (var measurementClass in MeasurementClasses)
            {
                SetAdditionalStatsBindingsCheck(expression, measurementClass.Name, rng);

                foreach (var measurementName in measurementClass.Measurements.Keys)
                {
                    SetAdditionalStatsBindingsCheck(expression, measurementName, rng);
                }
            }

            foreach (var measurementClass in MeasurementClasses)
            {
                foreach (var measurementName in measurementClass.Measurements.Keys)
                {
                    expression.Bind(measurementName, 1.123456 + rng.NextDouble());
                }
            }
        }

        private static void SetAdditionalVariablesBindings(Expression expression, IDictionary<string, double> evaluatedVariables, IDictionary<string, StatsInfo> variablesStatistics)
        {
            foreach (var variableName in variablesStatistics.Keys)
            {
                var tuple = variablesStatistics[variableName];

                var value = evaluatedVariables[variableName];

                double sigma = tuple.mad > 0 ? (value - tuple.median) / tuple.mad : 0.0;
                expression.Bind(variableName + "Sigma", sigma);

                expression.Bind(variableName + "Min", tuple.min);
                expression.Bind(variableName + "Max", tuple.max);
                expression.Bind(variableName + "Median", tuple.median);
            }
        }

        private static void SetAdditionalVariablesBindingsCheck(Expression expression, Random rng, List<Variable> variables)
        {
            HashSet<string> usedVariables;
            try
            {
                usedVariables = new HashSet<string>(expression.getVariables());
            }
            catch (Exception)
            {
                usedVariables = new HashSet<string>();
            }

            foreach (var variable in variables)
            {
                var variableName = variable.Name;

                expression.Bind(variableName + "Sigma", 1.123456 + rng.NextDouble());
                expression.Bind(variableName + "Min", 1.123456 + rng.NextDouble());
                expression.Bind(variableName + "Max", 1.123456 + rng.NextDouble());
                expression.Bind(variableName + "Median", 1.123456 + rng.NextDouble());

                if (usedVariables.Contains(variableName + "Sigma") || usedVariables.Contains(variableName + "Min") || usedVariables.Contains(variableName + "Max") || usedVariables.Contains(variableName + "Median"))
                {
                    variable.StatisticsRequired = true;
                }
            }
        }

        private string PreprocessFormula(string formula)
        {
            var lines = formula.Split(Environment.NewLine);
            StringBuilder sb = new();
            foreach (var line in lines)
            {
                if (!line.Trim().StartsWith("#"))
                {
                    sb.Append(line);
                }
                else
                {
                    // Add padding so that positions match when parsing
                    for (int i = 0; i < line.Length; i++)
                    {
                        sb.Append(' ');
                    }
                }
            }
            return sb.ToString();
        }

        private class Variable
        {
            public string Name { get; }
            public int Position { get; }
            public StringBuilder Formula { get; set; } = new();

            public Expression? Expression { get; set; }

            public bool StatisticsRequired { get; set; }

            public Variable(string name, int position)
            {
                Name = name;
                Position = position;
                // Add padding so that positions match when parsing
                for (int i = 0; i < position; i++)
                {
                    Formula.Append(' ');
                }
            }
        }

        private bool BuildVariable(Variable variable, List<Variable> variables, out string? errorMessage)
        {
            errorMessage = null;

            string preprocessed = PreprocessFormula(variable.Formula.ToString());

            variable.Formula = new StringBuilder(preprocessed);
            variable.Expression = new Expression(preprocessed);

            // Disable string functions
            variable.Expression.DisableFunction(new string[]{
                "LEFT", "RIGHT", "MID", "REVERSE",
                "ISNUMBER", "LOWER", "UPPER", "PROPER",
                "TRIM", "LEN", "TEXT", "REPLACE",
                "SUBSTITUTE", "FIND", "SEARCH", "CONCAT",
                "ISBLANK", "REPT", "CHAR", "CODE", "VALUE"
            });

            // Disable date & time functions
            variable.Expression.DisableFunction(new string[]
            {
                "DATE", "DATEVALUE", "TIME", "SECOND",
                "MINUTE", "HOUR", "DAY", "MONTH",
                "YEAR", "NOW", "TODAY", "EDATE",
                "WEEKDAY", "WEEKNUM", "WORKDAY",
                "NETWORKDAYS", "EOMONTH", "DATEDIF", "DAYS"
            });

            var rng = new Random();

            SetStatsBindingsCheck(variable.Expression, rng);

            foreach (var var in variables)
            {
                var variableName = var.Name;
                variable.Expression.Bind(variableName, 1.123456 + rng.NextDouble());
            }

            SetAdditionalVariablesBindingsCheck(variable.Expression, rng, variables);

            var errors = variable.Expression.GetError();

            if (errors != null && errors.Count > 0)
            {
                StringBuilder sb = new();

                if (errors != null)
                {
                    foreach (var error in errors)
                    {
                        sb.AppendLine(error);
                    }
                }

                errorMessage = sb.ToString();
                return false;
            }
            else
            {
                try
                {
                    // Evaluate to check if all used variables exist
                    variable.Expression.Eval();
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                    return false;
                }
            }

            return true;
        }

        public bool Build(string formula, out IEvaluationService.IEvaluator? evaluator, out string? errorMessage)
        {
            bool canDefineVariables = true;
            var variableDefinitionRegex = new Regex("^[\\s]*[A-Za-z0-9]+[\\s]*:=");
            Variable? currentVariableDefinition = null;
            List<Variable> variables = new();

            StringBuilder evaluationFormula = new();

            var lines = formula.Split(Environment.NewLine);
            int position = 0;

            foreach (var l in lines)
            {
                bool appendedToFormula = false;

                if (currentVariableDefinition != null)
                {
                    string trim = l.Trim();
                    if (!trim.StartsWith("#") && trim.EndsWith(';'))
                    {
                        currentVariableDefinition.Formula.AppendLine(l.Substring(0, l.LastIndexOf(';')));
                        currentVariableDefinition = null;
                    }
                    else
                    {
                        currentVariableDefinition.Formula.AppendLine(l);
                    }
                }
                else
                {
                    if (canDefineVariables && variableDefinitionRegex.IsMatch(l))
                    {
                        string line = l;

                        int assignmentTokenIndex = line.IndexOf(":=");

                        bool isSingleLineDefinition = false;

                        if (line.Trim().EndsWith(';'))
                        {
                            line = line.Substring(0, line.LastIndexOf(';'));
                            isSingleLineDefinition = true;
                        }

                        currentVariableDefinition = new Variable(line.Substring(0, assignmentTokenIndex).Trim(), position + assignmentTokenIndex + 2);
                        currentVariableDefinition.Formula.AppendLine(line.Substring(assignmentTokenIndex + 2, line.Length - (assignmentTokenIndex + 2)));

                        variables.Add(currentVariableDefinition);

                        if (isSingleLineDefinition)
                        {
                            currentVariableDefinition = null;
                        }
                    }
                    else
                    {
                        evaluationFormula.AppendLine(l);
                        appendedToFormula = true;

                        var trim = l.Trim();
                        if (trim.Length > 0 && !trim.StartsWith("#"))
                        {
                            // Only allow defining variables before evaluation formula
                            canDefineVariables = false;
                        }
                    }
                }

                if (!appendedToFormula)
                {
                    // Add padding so that positions match when parsing
                    for (int i = 0; i < l.Length; i++)
                    {
                        evaluationFormula.Append(' ');
                    }
                }

                position += l.Length;
            }

            // Add evaluation variable which produces the final result
            Variable evaluatonVariable = new("Result", 0);
            evaluatonVariable.Formula.Append(evaluationFormula.ToString());
            variables.Add(evaluatonVariable);

            evaluator = null;
            errorMessage = null;

            List<Variable> evaluatedVariables = new();

            foreach (Variable variable in variables)
            {
                if (!BuildVariable(variable, evaluatedVariables, out errorMessage))
                {
                    return false;
                }

                evaluatedVariables.Add(variable);
            }

            evaluator = new Evaluator(this, variables, formula);
            return true;
        }
    }
}
