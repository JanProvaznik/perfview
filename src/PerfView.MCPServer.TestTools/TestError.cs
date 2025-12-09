using Microsoft.Extensions.Logging;
using PerfView.MCPServer.Tools;
using System;
using System.Text.Json;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger("Test");
var tools = new PerfViewTools(logger);

Console.WriteLine("Testing error handling with missing parameter:");
var badParams = JsonDocument.Parse(@"{ ""output_path"": ""./test.etl"" }").RootElement;
var result = tools.CollectCpuTrace(badParams);
Console.WriteLine(result);
