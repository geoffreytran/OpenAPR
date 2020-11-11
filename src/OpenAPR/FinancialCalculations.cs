using System;
using System.Collections.Generic;
using System.Text;

namespace OpenAPR
{
    public static class FinancialCalculations
    {
        internal static double GetPresentValueInterestFactor(PeriodSpan span, double apr, double periodsPerYear, double daysPerPeriod)
        {
            return (double)(1 / (Math.Pow((1 + (apr / periodsPerYear)), (double)span.Periods) * (1 + ((span.OddDays / daysPerPeriod) * (apr / periodsPerYear)))));
        }

        internal static double GetPresentValueInterestFactorAnnuity(DateTime startDate, DateTime currLiDate, UnitPeriod frequency, double apr, double periodsPerYear, double daysPerPeriod, int numberOccurrences, UnitPeriod commonPeriod)
        {
            var sb = new StringBuilder();
            const int ic = 1;
            var pvifa = 0.0d; //return value, running tally of PVIF
            PeriodSpan lastSpan;
            var currSpan = DateTimeCalculations.GetNumberPeriods(startDate, currLiDate, commonPeriod);
            lastSpan.OddDays = 0;
            lastSpan.Periods = 0;
            for (var i = 0; i < numberOccurrences; i++)
            {
                //get the PVIF for this current item and add to the pvifa
                pvifa += GetPresentValueInterestFactor(currSpan, apr, periodsPerYear, daysPerPeriod);
                sb.AppendLine(ic.ToString() + "   " + currLiDate.ToString() + "    " + currSpan.Periods.ToString() + currSpan.OddDays.ToString() + "    " + pvifa.ToString());
                //TODO... figure out how to determine the recurrence in periods if 
                //periods other than a monthly type or annual are passed in.  
                //Perhaps we should restrict to only dates instead?
                lastSpan = currSpan;
                currLiDate = DateTimeCalculations.AddPeriodToDate(currLiDate, frequency);
                currSpan = DateTimeCalculations.GetNumberPeriods(startDate, currLiDate, commonPeriod);
            }
            return pvifa;
        }

        //Gets the a-umlaut over X in Reg-Z Appendix J
        internal static double GetPresentValueInterestFactorAnnuityStream(double apr, double periodsPerYear, int startingPeriod, int numberOfPeriods)
        {
            var pvifa = 0.0d; //return value, running tally of PVIF

            var currPeriod = startingPeriod;

            while (currPeriod < startingPeriod + numberOfPeriods)
            {
                pvifa += (double)(1 / Math.Pow(1 + (apr / periodsPerYear), currPeriod));
                currPeriod += 1;
            }
            return pvifa;
        }
    }
}
