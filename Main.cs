/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;

static class Program
{
	static void Main()
	{
		Stopwatch sw = Stopwatch.StartNew();
        int Ntrials = 1000;
        int Ndim = 4;
        string setname = "Dense4";

        Random random = new Random();
        Random rnd = new Random();
        XDocument xdoc = XDocument.Load("samples.xml");

        foreach (XElement xelem in xdoc.Root.Elements("overlapping", "simpletiled"))
        {
            List<double[]> WeightCollect = new List<double[]>();

            for (int il = 0; il < Ntrials; il++)
            {
                double[] LogWeightCollect = new double[Ndim];
                double SumOfLogWeights = 0.0;
                double mindistsq = 1.0;
                for (int kk = 0; kk < Ndim; kk++)
                {
                    LogWeightCollect[kk] = -Math.Log(1 - rnd.NextDouble());
                    SumOfLogWeights += LogWeightCollect[kk];
                }

                for (int kk = 0; kk < Ndim; kk++) LogWeightCollect[kk] /= SumOfLogWeights;

                if (il == 0)
                {
                    WeightCollect.Add(LogWeightCollect);
                }
                else
                {
                    for (int mm = 0; mm < WeightCollect.Count; mm++)
                    {
                        double dist = 0;
                        for (int kk = 0; kk < Ndim; kk++) dist += Math.Pow(LogWeightCollect[kk] - WeightCollect[mm][kk], 2);
                        if (dist < mindistsq) mindistsq = dist;
                    }

                    if (mindistsq >= 0.01)
                    {
                        WeightCollect.Add(LogWeightCollect);
                    }
                }
            }

            int counter = 1;
            for (int it = 0; it < WeightCollect.Count; it++)
            {

                Dictionary<string, double> MargFreq = new Dictionary<string, double>();
                MargFreq.Add("t", WeightCollect[it][0]);
                MargFreq.Add("cross", WeightCollect[it][1]);
                MargFreq.Add("line", WeightCollect[it][2]);
                MargFreq.Add("corner", WeightCollect[it][3]);

                MargFreq.Add("x", 0.0);
                MargFreq.Add("v", 0.0);
                MargFreq.Add("skew", 0.0);
                MargFreq.Add("empty", 0.0);

                Model model;
                string name = xelem.Get<string>("name");
                string filename = name + $"_{it}";
                Console.WriteLine($"< {filename}");

                if (xelem.Name == "overlapping") model = new OverlappingModel(name, xelem.Get("N", 2), xelem.Get("width", 48), xelem.Get("height", 48),
                    xelem.Get("periodicInput", true), xelem.Get("periodic", false), xelem.Get("symmetry", 8), xelem.Get("ground", 0));
                else if (xelem.Name == "simpletiled") model = new SimpleTiledModel(name, xelem.Get<string>("subset"),
                    xelem.Get("width", 10), xelem.Get("height", 10), xelem.Get("periodic", true), xelem.Get("black", false), MargFreq);
                else continue;

                for (int i = 0; i < xelem.Get("screenshots", 3); i++)
                {
                    for (int k = 0; k < 10; k++)
                    {
                        Console.Write("> ");
                        int seed = random.Next();
                        bool finished = model.Run(seed, xelem.Get("limit", 0));
                        if (finished)
                        {
                            Console.WriteLine("DONE"); 

                            //if (i<10) model.Graphics().Save($"C:\\Users\\liuke\\Documents\\AutoMat\\{setname}\\{filename} {i}.png");
                            if (model is SimpleTiledModel && xelem.Get("textOutput", true))
                                System.IO.File.WriteAllText($"C:\\Users\\liuke\\Documents\\AutoMat\\{setname}\\encode {filename} {i}.txt", (model as SimpleTiledModel).TextOutput());

                            break;
                        }
                        else Console.WriteLine("CONTRADICTION");
                    }
                }
                counter++;

                Console.WriteLine($"time = {sw.ElapsedMilliseconds}");
            }
            var weightstext = new System.Text.StringBuilder();

            for (int it = 0; it < WeightCollect.Count; it++)
            {
                for (int kk = 0; kk < Ndim; kk++)
                {
                    weightstext.Append(WeightCollect[it][kk].ToString() + ",");
                }
                weightstext.Append(Environment.NewLine);
            }
            System.IO.File.WriteAllText($"C:\\Users\\liuke\\Documents\\AutoMat\\{setname}\\{setname} weights.txt", weightstext.ToString());
        }
	}
}
