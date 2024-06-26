﻿// See https://aka.ms/new-console-template for more information
using Microsoft.ML.OnnxRuntimeGenAI;

OgaHandle ogaHandle = new OgaHandle();

Console.WriteLine("-------------");
Console.WriteLine("Hello, Phi!");
Console.WriteLine("-------------");

string modelPath = "../../../../../../models";//Path.Combine(Directory.GetCurrentDirectory(), "models");
//rename files
var files = Directory.GetFiles(modelPath,"*.*");
foreach(string file in files){
    var newfile = file.Replace("cpu_and_mobile_cpu-int4-rtn-block-32-acc-level-4_","");
    File.Move(file,newfile);
}
using Model model = new Model(modelPath);
using Tokenizer tokenizer = new Tokenizer(model);

Console.WriteLine("Please enter option number:");
Console.WriteLine("1. Complete Output");
Console.WriteLine("2. Streaming Output");
int.TryParse(Console.ReadLine(), out var option);

while (true)
{
    Console.WriteLine("Prompt:");
    // Example prompt:
    // "def is_prime(num):"
    string prompt = Console.ReadLine();
    var sequences = tokenizer.Encode(prompt);

    using GeneratorParams generatorParams = new GeneratorParams(model);
    generatorParams.SetSearchOption("max_length", 200);
    generatorParams.SetInputSequences(sequences);

    if (option == 1) // Complete Output
    {
        var outputSequences = model.Generate(generatorParams);
        var outputString = tokenizer.Decode(outputSequences[0]);

        Console.WriteLine("Output:");
        Console.WriteLine(outputString);
    }

    else if (option == 2) //Streaming Output
    {
        using var tokenizerStream = tokenizer.CreateStream();
        using var generator = new Generator(model, generatorParams);
        while (!generator.IsDone())
        {
            generator.ComputeLogits();
            generator.GenerateNextToken();
            Console.Write(tokenizerStream.Decode(generator.GetSequence(0)[^1]));
        }
    }
}