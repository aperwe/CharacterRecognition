#region Copyright (c), Some Rights Reserved
/*##########################################################################
 * 
 * IBackPropagation.cs
 * -------------------------------------------------------------------------
 * By
 * Murat FIRAT, September 2007
 * 
 * -------------------------------------------------------------------------
 ###########################################################################*/
#endregion

using System;
using System.Collections.Generic;
namespace BPSimplified.Lib
{

    interface IBackPropagation<T>
    {
        void BackPropagate();
        double F(double x);
        void ForwardPropagate(double[] pattern, T output);
        double GetError();
        void InitializeNetwork(Dictionary<T, double[]> TrainingSet);
        void Recognize(double[] Input, ref T MatchedHigh, ref double OutputValueHight,
                                        ref T MatchedLow, ref double OutputValueLow);
    }
}
