﻿namespace NCalc
{
    using System;

    public class EvaluationException : Exception
    {
        public EvaluationException(string message) : base(message) {}

        public EvaluationException(string message, Exception innerException) : base(message, innerException) {}
    }
}