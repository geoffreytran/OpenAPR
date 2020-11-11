using System;
using System.Collections.Generic;
using System.Text;

namespace APRCalculator
{
    public static class FinancialCalculations
    {
        internal static double GetPVIF(PeriodSpan span, double APR, double PeriodsPerYear, double DaysPerPeriod)
        {
            return (double)(1 / (Math.Pow((1 + (APR / PeriodsPerYear)), (double)span.Periods) * (1 + ((span.OddDays / DaysPerPeriod) * (APR / PeriodsPerYear)))));
        }

        internal static double GetPVIFA(DateTime StartDate, DateTime CurrLIDate, UnitPeriod frequency, double APR, double PeriodsPerYear, double DaysPerPeriod, int NumberOccurrences, UnitPeriod CommonPeriod)
        {
            StringBuilder sb = new StringBuilder();
            int ic = 1;
            double pvifa = 0.0d; //return value, running tally of PVIF
            PeriodSpan lastSpan;
            PeriodSpan currSpan = DateTimeCalculations.GetNumberPeriods(StartDate, CurrLIDate, CommonPeriod);
            lastSpan.OddDays = 0;
            lastSpan.Periods = 0;
            for (int i = 0; i < NumberOccurrences; i++)
            {
                //get the PVIF for this current item and add to the pvifa
                pvifa += GetPVIF(currSpan, APR, PeriodsPerYear, DaysPerPeriod);
                sb.AppendLine(ic.ToString() + "   " + CurrLIDate.ToString() + "    " + currSpan.Periods.ToString() + currSpan.OddDays.ToString() + "    " + pvifa.ToString());
                //TODO... figure out how to determine the recurrence in periods if 
                //periods other than a monthly type or annual are passed in.  
                //Perhaps we should restrict to only dates instead?
                lastSpan = currSpan;
                CurrLIDate = DateTimeCalculations.AddPeriodToDate(CurrLIDate, frequency);
                currSpan = DateTimeCalculations.GetNumberPeriods(StartDate, CurrLIDate, CommonPeriod);
            }
            return pvifa;
        }

        //Gets the a-umlaut over X in Reg-Z Appendix J
        internal static double GetPVIFAStream(double APR, double PeriodsPerYear, int StartingPeriod, int NumberOfPeriods)
        {
            double pvifa = 0.0d; //return value, running tally of PVIF

            int currPeriod = StartingPeriod;

            while (currPeriod < StartingPeriod + NumberOfPeriods)
            {
                pvifa += (double)(1 / Math.Pow(1 + (APR / PeriodsPerYear), currPeriod)); 
            }
            return pvifa;
        }
    }
}
