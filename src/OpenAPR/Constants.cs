using System;
using System.Collections.Generic;
using System.Text;

namespace APRCalculator
{
    public struct UnitPeriod
    {
        public UnitPeriodType periodType;
        public int numPeriods;
    }

    public enum UnitPeriodType
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    public struct PeriodSpan
    {
        public int Periods;
        public int OddDays;
    }

    public class Constants
    {
    }
}
