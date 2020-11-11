using System;
using System.Collections.Generic;
using System.Text;

namespace OpenAPR
{
    public struct UnitPeriod
    {
        public UnitPeriodType PeriodType;
        public int NumPeriods;
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
}
