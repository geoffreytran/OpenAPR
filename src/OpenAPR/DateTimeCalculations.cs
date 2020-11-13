using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAPR
{
  public static class DateTimeCalculations
  {
    /// <summary>
    /// Gets a Date based on a given period in the Loan Lifecycle.
    /// </summary>
    internal static DateTime GetDateFromPeriod(PeriodSpan span, DateTime startDate, UnitPeriod commonPeriod)
    {
      try
      {
        return commonPeriod.PeriodType switch
        {
          UnitPeriodType.Monthly => startDate.AddMonths(commonPeriod.NumPeriods * span.Periods)
              .AddDays(span.OddDays),
          UnitPeriodType.Yearly => startDate.AddYears(commonPeriod.NumPeriods * span.Periods)
              .AddDays(span.OddDays),
          UnitPeriodType.Weekly => startDate.AddDays(commonPeriod.NumPeriods * span.Periods * 7)
              .AddDays(span.OddDays),
          _ => startDate.AddDays(commonPeriod.NumPeriods * span.Periods)
        };
      }
      catch (Exception ex)
      {
        ApplicationException newex = new ApplicationException("Exception in DateTimeCalculations.GetDateFromPeriod", ex);
        throw (newex);
      }
    }

    /// <summary>
    /// The number of periods per year for a given UnitPeriod
    /// </summary>
    internal static double PeriodsPerYear(UnitPeriod period)
    {
      return period.PeriodType switch
      {
        UnitPeriodType.Monthly => Convert.ToDouble(12d / period.NumPeriods),
        UnitPeriodType.Weekly => Convert.ToDouble(52d / period.NumPeriods),
        UnitPeriodType.Yearly => 1d,//The maximum UnitPeriod is 1 year, so this is always 1 (as opposed to say .5 for a 2 year common period)
        UnitPeriodType.Daily => Convert.ToDouble(365d / period.NumPeriods),
        _ => throw new ApplicationException("Invalid Unit Period Type passed into DaysPerPeriod"),
      };
    }

    /// <summary>
    /// Returns the Days in a UnitPeriod type
    /// </summary>
    internal static int DaysPerPeriod(UnitPeriod period)
    {
      return period.PeriodType switch
      {
        UnitPeriodType.Monthly => 30 * period.NumPeriods,
        UnitPeriodType.Weekly => 7 * period.NumPeriods,
        UnitPeriodType.Yearly => 365 * period.NumPeriods,
        UnitPeriodType.Daily => period.NumPeriods,
        _ => throw new ApplicationException("Invalid Unit Period Type passed into DaysPerPeriod"),
      };
    }

    /// <summary>
    /// Gets a number of periods between two dates based on the common period
    /// </summary>
    internal static PeriodSpan GetNumberPeriods(DateTime startDate, DateTime currentDate, UnitPeriod commonPeriod)
    {
      try
      {
        PeriodSpan psReturn;
        switch (commonPeriod.PeriodType)
        {
          case UnitPeriodType.Monthly:
            {
              var iMonths = DiffMonths(startDate, currentDate);
              psReturn.Periods = Convert.ToInt32(System.Math.Round(Convert.ToDouble(iMonths / commonPeriod.NumPeriods), 0));
              var dtTemp = startDate.AddMonths(psReturn.Periods * commonPeriod.NumPeriods);
              var ts = currentDate - dtTemp;
              psReturn.OddDays = ts.Days;
              return psReturn;
            }
          case UnitPeriodType.Yearly:
            {
              var iYears = DiffYears(startDate, currentDate);
              psReturn.Periods = iYears; //1 year is always the max period, so we can just return the # of years
              var ts = currentDate - startDate.AddYears(psReturn.Periods);
              psReturn.OddDays = ts.Days;
              return psReturn;
            }
          default:
            {
              var ts = currentDate - startDate;
              var iDays = ts.Days;
              if (commonPeriod.PeriodType == UnitPeriodType.Weekly)
              {
                var iWeeks = DiffWeeks(startDate, currentDate);
                psReturn.Periods = Convert.ToInt32(System.Math.Round(Convert.ToDouble(iWeeks / commonPeriod.NumPeriods)));
                ts = currentDate - startDate.AddDays(psReturn.Periods * commonPeriod.NumPeriods * 7);
                psReturn.OddDays = ts.Days;
              }
              else
              {
                psReturn.Periods = Convert.ToInt32(System.Math.Round(Convert.ToDouble(iDays / commonPeriod.NumPeriods)));
                ts = currentDate - startDate.AddDays(psReturn.Periods * commonPeriod.NumPeriods);
                psReturn.OddDays = ts.Days;
              }
              return psReturn;
            }
        }
      }
      catch (Exception ex)
      {
        ApplicationException newex = new ApplicationException("Error in DateTimeCalculations.GetNumberPeriods", ex);
        throw (newex);
      }
    }

    /// <summary>
    /// Returns a difference in whole months between two dates.  It rounds down to the nearest whole number.
    /// </summary>
    internal static int DiffMonths(DateTime startDate, DateTime currentDate)
    {
      return ((currentDate.Year - startDate.Year) * 12) + (currentDate.Month - startDate.Month) - (currentDate.Day < startDate.Day ? 1 : 0);
    }

    /// <summary>
    /// Returns a difference in whole years between two dates.  It rounds down to the nearest whole number.
    /// </summary>
    internal static int DiffYears(DateTime startDate, DateTime currentDate)
    {
      return (currentDate.Year - startDate.Year) - (currentDate.DayOfYear < startDate.DayOfYear ? 1 : 0);
    }

    /// <summary>
    /// Returns a difference in whole weeks between two dates.  It rounds down to the nearest whole number.
    /// </summary>
    internal static int DiffWeeks(DateTime startDate, DateTime currentDate)
    {
      var ts = currentDate - startDate;
      return Convert.ToInt32(System.Math.Round(Convert.ToDouble(ts.Days / 7), 0));
    }

    /// <summary>
    /// Calculates the Common Period for this APR calculation
    /// </summary>
    public static UnitPeriod CalculateCommonPeriod(LineItemCollection coll)
    {
      var h = new System.Collections.Generic.Dictionary<string, int>();
      var hCommDay = new System.Collections.Generic.Dictionary<string, int>();
      var hDaysInType = new System.Collections.Generic.Dictionary<string, int>();
      var bAllMonths = true;
      var iPerCount = 0;
      coll.Sort();
      try
      {
        int iDays;
        var i = 1;
        for (i = 1; i < coll.Count; i++)
        {
          //no common period will exist if the two line items are 
          //on the same date, so we skip it if that is the case
          if (coll[i - 1].Date == coll[i].Date) continue;
          var liPrior = coll[i - 1];
          var li = coll[i];
          var ts = li.Date - liPrior.Date;
          iDays = ts.Days;
          //if the day of the month is the same between
          //the two line items are in a month type period (unless > 1 year)
          //monthly will usually be the most common period type, so we check 
          //that one first
          var iPeriods = 0;
          string sKey;
          if (liPrior.Date.Day == li.Date.Day)
          {
            iPeriods = DiffMonths(liPrior.Date, li.Date);
            if (iPeriods >= 12)
            {
              iPeriods = 1;
              sKey = "1Y";
            }
            else
            {
              sKey = iPeriods.ToString() + "M";
            }
          }
          //If this is more than 365 days, then it is yearly
          else if (iDays > 365)
          {
            iPeriods = 1;
            sKey = "1Y";
            bAllMonths = false;
          }
          //if this is more than 6 days, but has not met the prior conditions
          //it is a weekly period
          else if (iDays > 6 && iDays % 7 == 0)
          //else if (iDays > 6)
          {
            iPeriods = DiffWeeks(liPrior.Date, li.Date);
            sKey = iPeriods.ToString() + "W";
            bAllMonths = false;
          }
          //otherwise it is a daily period type
          else
          {
            iPeriods = iDays;
            sKey = iPeriods.ToString() + "D";
            bAllMonths = false;
          }

          //now we increment keys in the hashtable
          if (h.ContainsKey(sKey))
          {
            h[sKey] = Convert.ToInt32(h[sKey]) + 1;
          }
          else
          {
            h.Add(sKey, 1);
            hDaysInType.Add(sKey, iDays);
            hCommDay.Add(sKey, li.Date.Day);
          }
          iPerCount++;
        } //finished counting up periods

        //Now we get the mode for the line item type
        short maxCount = 0;
        var sPerTypeMode = string.Empty;
        foreach (var k in h.Keys.Where(k => h[k] > maxCount ||
                                            (h[k] == maxCount && (int)hDaysInType[sPerTypeMode] > (int)hDaysInType[k])))
        {
          maxCount = Convert.ToInt16(h[k]);
          sPerTypeMode = k;
        }

        //Now we check to see whether we came up with a common period
        //if not, then we take the average of all periods
        if (maxCount <= 1 && h.Count > 1)
        {
          //get the end date
          var dtEndDate = coll[^1].Date;

          //if the period count is Zero, we can't divide by it, so set it to 1
          if (iPerCount < 1) { iPerCount = 1; }

          //get the average number of days between events
          var tsDays = dtEndDate - coll[0].Date;
          iDays = Convert.ToInt32(System.Math.Round(Convert.ToDouble(tsDays.Days / iPerCount), 0));

          sPerTypeMode = "1Y";
          if (iDays < 365)
          {
            if (bAllMonths && DiffMonths(coll[0].Date, dtEndDate) % iPerCount == 0)
            {
              iDays = DiffMonths(coll[0].Date, dtEndDate);
              sPerTypeMode = iDays.ToString() + "M";
            }
            else if (iDays >= 7)
            {
              sPerTypeMode = System.Math.Round((double)(iDays / 7), 0).ToString() + "W";
            }
            else
            {
              sPerTypeMode = iDays.ToString() + "D";
            }
          }
        }
        UnitPeriod upReturn;
        if (sPerTypeMode.Contains("Y"))
        {
          upReturn.NumPeriods = 1;
          upReturn.PeriodType = UnitPeriodType.Yearly;
        }
        else if (sPerTypeMode.Contains("M"))
        {
          upReturn.NumPeriods = int.Parse(sPerTypeMode.Replace("M", string.Empty));
          upReturn.PeriodType = UnitPeriodType.Monthly;
        }
        else if (sPerTypeMode.Contains("W"))
        {
          upReturn.NumPeriods = int.Parse(sPerTypeMode.Replace("W", string.Empty));
          upReturn.PeriodType = UnitPeriodType.Weekly;
        }
        else
        {
          upReturn.NumPeriods = int.Parse(sPerTypeMode.Replace("D", string.Empty));
          upReturn.PeriodType = UnitPeriodType.Daily;
        }
        return upReturn;
      }
      catch (Exception ex)
      {
        ApplicationException newex = new ApplicationException("Error in DateTimeCalculations.CalculateCommonPeriod", ex);
        throw (newex);
      }
      finally
      {
        h = null;
        hCommDay = null;
        hDaysInType = null;
      }
    }

    /// <summary>
    /// Get a date based on start date, timespan and something
    /// </summary>
    internal static DateTime AddPeriodToDate(DateTime currDate, UnitPeriod period)
    {
      return period.PeriodType switch
      {
        UnitPeriodType.Monthly => currDate.AddMonths(period.NumPeriods),
        UnitPeriodType.Weekly => currDate.AddDays(period.NumPeriods * 7),
        UnitPeriodType.Daily => currDate.AddDays(period.NumPeriods),
        UnitPeriodType.Yearly => currDate.AddYears(period.NumPeriods),
        _ => throw new ApplicationException("Invalid Period Type passed in"),
      };
    }

    internal static string GetUnitPeriodString(UnitPeriod period)
    {
      return period.PeriodType switch
      {
        UnitPeriodType.Daily => period.NumPeriods.ToString() + "D",
        UnitPeriodType.Weekly => period.NumPeriods.ToString() + "W",
        UnitPeriodType.Monthly => period.NumPeriods.ToString() + "M",
        UnitPeriodType.Yearly => period.NumPeriods.ToString() + "Y",
        _ => throw new ApplicationException("Invalid Period Type in getUnitPeriodString"),
      };
    }

    #region "VBCode"
    //            For i = 1 To Me.maLineItems.Length - 1
    //                'There is no common period if there are two line items on the same date, so we skip consecutive line items
    //                'on the same date
    //                If maLineItems(i - 1).dtDate <> maLineItems(i).dtDate Then
    //                    'Get the actual days between the two dates
    //                    iDays = VB.DateDiff(DateInterval.DayOfYear, maLineItems(i - 1).dtDate, maLineItems(i).dtDate)
    //                    If maLineItems(i - 1).dtDate.Day = maLineItems(i).dtDate.Day Then 'If this is a monthly interval
    //                        iPeriods = VB.DateDiff(DateInterval.Month, maLineItems(i - 1).dtDate, maLineItems(i).dtDate)
    //                        sKey = CStr(iPeriods) & "M" 'Denote this as Monthly # of periods
    //                    ElseIf maLineItems(i - 1).dtDate.DayOfWeek <> maLineItems(i).dtDate.DayOfWeek Then 'If this is NOT weekly
    //                        bAllMonths = False
    //                        If iDays < 365 Then 'Daily period
    //                            iPeriods = iDays
    //                            sKey = CStr(iDays) & "D"
    //                        Else ' Yearly periods
    //                            iPeriods = System.Math.Round(iDays / 365, 0)
    //                            sKey = "1Y"
    //                        End If
    //                    ElseIf maLineItems(i - 1).dtDate.DayOfWeek = maLineItems(i).dtDate.DayOfWeek Then 'If this is a weekly interval
    //                        bAllMonths = False
    //                        iPeriods = VB.DateDiff(DateInterval.DayOfYear, maLineItems(i - 1).dtDate, maLineItems(i).dtDate) / 7
    //                        sKey = CStr(iPeriods) & "W" 'Denote this as a Weekly # of periods
    //                    End If

    //                    If h.ContainsKey(sKey) Then
    //                        h.Item(sKey) = CInt(h.Item(sKey)) + 1
    //                    Else
    //                        h.Add(sKey, 1)
    //                        hDaysInType.Add(sKey, iDays)
    //                        hCommDay.Add(sKey, maLineItems(i).dtDate.Day)
    //                    End If
    //                    iPerCount += 1
    //                End If
    //            Next

    //            If Me.miAprMethod = Me.CONST_INTERIMAPR Then
    //                i = maLineItems.Length - 1
    //                If maLineItems(i).dtDate <> Me.mdtEntersRepayment Then
    //                    'Get the actual days between the two dates
    //                    iDays = VB.DateDiff(DateInterval.DayOfYear, maLineItems(i).dtDate, Me.mdtEntersRepayment)
    //                    If maLineItems(i).dtDate.Day = Me.mdtEntersRepayment.Day Then 'If this is a monthly interval
    //                        iPeriods = VB.DateDiff(DateInterval.Month, maLineItems(i).dtDate, Me.mdtEntersRepayment)
    //                        sKey = CStr(iPeriods) & "M" 'Denote this as Monthly # of periods
    //                    ElseIf maLineItems(i).dtDate.DayOfWeek <> Me.mdtEntersRepayment.DayOfWeek Then 'If this is NOT weekly
    //                        bAllMonths = False
    //                        If iDays < 365 Then 'Daily period
    //                            iPeriods = iDays
    //                            sKey = CStr(iDays) & "D"
    //                        Else ' Yearly periods
    //                            iPeriods = System.Math.Round(iDays / 365, 0)
    //                            sKey = "1Y"
    //                        End If
    //                    ElseIf maLineItems(i).dtDate.DayOfWeek = Me.mdtEntersRepayment.DayOfWeek Then 'If this is a weekly interval
    //                        bAllMonths = False
    //                        iPeriods = VB.DateDiff(DateInterval.DayOfYear, maLineItems(i).dtDate, Me.mdtEntersRepayment) / 7
    //                        sKey = CStr(iPeriods) & "W" 'Denote this as a Weekly # of periods
    //                    End If

    //                    If h.ContainsKey(sKey) Then
    //                        h.Item(sKey) = CInt(h.Item(sKey)) + 1
    //                    Else
    //                        h.Add(sKey, 1)
    //                        hDaysInType.Add(sKey, iDays)
    //                        hCommDay.Add(sKey, Me.mdtEntersRepayment.DayOfYear)
    //                    End If
    //                    iPerCount += 1
    //                End If
    //            End If

    //            'And now we get the most frequent interval (The MODE in statistics...)
    //            Dim maxCount As Int16 = 0
    //            Dim sPerTypeMode As String
    //            For Each de As DictionaryEntry In h
    //                If de.Value > maxCount OrElse (de.Value = maxCount AndAlso hDaysInType(sPerTypeMode) > hDaysInType(de.Key)) Then
    //                    If bDebugFlag Then sw.WriteLine(CStr(de.Key) & " has a count value of " & CStr(de.Value) & " which is > current maxValue or is equal and of shorter duration than " & sPerTypeMode & " the new Mode is " & CStr(de.Key))
    //                    maxCount = CInt(de.Value)
    //                    sPerTypeMode = de.Key
    //                End If
    //            Next


    //            'If there is no common period, then take the average
    //            If maxCount <= 1 AndAlso h.Count > 1 Then
    //                'Set the last date of the period according to the APR type
    //                Dim dtEndDate As DateTime
    //                If Me.miAprMethod = Me.CONST_INTERIMAPR Then
    //                    dtEndDate = Me.mdtEntersRepayment
    //                Else
    //                    dtEndDate = maLineItems(maLineItems.Length - 1).dtDate
    //                End If
    //                'If the period count wound up being Zero we change it to 1 to avoid arithmetic errors
    //                If iPerCount = 0 Then iPerCount = 1

    //                If bDebugFlag Then sw.WriteLine("There is no common period, we must take an average")

    //                'Get the average number of days between events
    //                iDays = Fix(DateDiff(DateInterval.DayOfYear, maLineItems(0).dtDate, dtEndDate) / iPerCount)

    //                If bDebugFlag Then sw.WriteLine(" Average Days Between Events is " & CStr(iDays))
    //                If bDebugFlag Then sw.WriteLine(" The number of timespans between events is " & CStr(iPerCount))

    //                'If the average number of days is > 1 year the common period defaults to 1 year
    //                'Otherwise, we do some more figuring
    //                If iDays < 365 Then
    //                    'If it is equally divisible by seven, then our common period is weekly
    //                    'In the rare case that a # of days yeilds both a monthly and weekly value, 
    //                    If iDays >= 7 Then
    //                        sPerTypeMode = CStr(Fix(iDays / 7)) & "W"
    //                    ElseIf bAllMonths Then
    //                        Dim iMonths As Int16 = DateDiff(DateInterval.Month, maLineItems(0).dtDate, dtEndDate)
    //                        If iMonths Mod iPerCount = 0 Then 'Only if the division yields whole months is this a monthly period type
    //                            iDays = iMonths / iPerCount
    //                            sPerTypeMode = CStr(iDays) & "M"
    //                        Else 'Otherwise it is still just days...
    //                            sPerTypeMode = CStr(iDays) & "D"
    //                        End If
    //                    Else
    //                        sPerTypeMode = CStr(iDays) & "D"
    //                    End If
    //                Else
    //                    sPerTypeMode = "1Y"
    //                End If
    //            End If

    //            'Give back the day of month for the common period
    //            If Right(sPerTypeMode, 1) = "M" OrElse Right(sPerTypeMode, 1) = "Y" Then
    //                iComPerDay = CInt(hCommDay(sPerTypeMode))
    //            Else
    //                iComPerDay = 0
    //            End If

    //            'Give Back the Number of items in the Mode
    //            iModeLength = maxCount

    //            If bDebugFlag Then sw.WriteLine("The Period Type is " & sPerTypeMode & " and the common day per period is " & CStr(iComPerDay))

    //            Me.miUnitsPerPeriod = CInt(sPerTypeMode.Remove(sPerTypeMode.Length - 1, 1))
    //            Me.msUnitType = Right(sPerTypeMode, 1)
    //            'Return the Type Mode
    //            Return sPerTypeMode

    #endregion
  }
}
