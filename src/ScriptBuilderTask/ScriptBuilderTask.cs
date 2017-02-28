﻿using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using NServiceBus.Persistence.Sql.ScriptBuilder;

namespace NServiceBus.Persistence.Sql
{
    public class ScriptBuilderTask : Task
    {
        BuildLogger logger;

        [Required]
        public string AssemblyPath { get; set; }

        [Required]
        public string IntermediateDirectory { get; set; }

        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public string SolutionDirectory { get; set; }

        public override bool Execute()
        {
            logger = new BuildLogger(BuildEngine);
            logger.LogInfo($"ScriptBuilderTask (version {typeof(ScriptBuilderTask).Assembly.GetName().Version}) Executing");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!ValidateInputs())
                {
                    return false;
                }
                Inner();
            }
            catch (ErrorsException exception)
            {
                logger.LogError(exception.Message, exception.FileName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception.ToFriendlyString());
            }
            finally
            {
                logger.LogInfo($"  Finished ScriptBuilderTask {stopwatch.ElapsedMilliseconds}ms.");
            }
            return !logger.ErrorOccurred;
        }

        bool ValidateInputs()
        {
            if (!File.Exists(AssemblyPath))
            {
                logger.LogError($"AssemblyPath '{AssemblyPath}' does not exist.");
                return false;
            }

            if (!Directory.Exists(IntermediateDirectory))
            {
                logger.LogError($"IntermediateDirectory '{IntermediateDirectory}' does not exist.");
                return false;
            }
            return true;
        }

        void Inner()
        {
            var moduleDefinition = ModuleDefinition.ReadModule(AssemblyPath, new ReaderParameters(ReadingMode.Deferred));
            var scriptPath = Path.Combine(IntermediateDirectory, "NServiceBus.Persistence.Sql");
            if (Directory.Exists(scriptPath))
            {
                Directory.Delete(scriptPath, true);
            }
            foreach (var variant in SqlVariantReader.Read(moduleDefinition))
            {
                Write(moduleDefinition, variant, scriptPath);
            }

            var customPath = OutputPathReader.Read(moduleDefinition);
            if (string.IsNullOrEmpty(customPath) == false)
            { 
                foreach (var variant in SqlVariantReader.Read(moduleDefinition))
                {
                    var outputPath = Path.Combine(customPath.Replace("$ProjectDir", ProjectDirectory).Replace("$SolutionDir", SolutionDirectory), variant.ToString());
                    Write(moduleDefinition, variant, outputPath);
                }
            }
        }

        void Write(ModuleDefinition moduleDefinition, BuildSqlVariant sqlVariant, string scriptPath)
        {
            Directory.CreateDirectory(scriptPath);
            SagaWriter.WriteSagaScripts(scriptPath, moduleDefinition, sqlVariant, logger);
            TimeoutWriter.WriteTimeoutScript(scriptPath, sqlVariant);
            SubscriptionWriter.WriteSubscriptionScript(scriptPath, sqlVariant);
            OutboxWriter.WriteOutboxScript(scriptPath, sqlVariant);
        }
    }
}