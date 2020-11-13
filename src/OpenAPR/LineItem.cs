using System;
using System.Collections.Generic;
using System.Text;

namespace OpenAPR
{
  #region "Enums"
  public enum LineItemType
  {
    Payment = 1,
    Disbursement = -1
  }

  public enum DateType
  {
    Periods,
    Date
  }
  #endregion

  public class LineItem
  {
    #region "private props"

    private double amount = 0.0d;
    private PeriodSpan span;
    private readonly DateType dateType;

    #endregion

    #region ".ctor"
    public LineItem(double amount, DateTime date, LineItemType liType)
    {
      if (amount < 0)
      {
        throw new ApplicationException("New Line Item must have an Amount >= 0");
      }
      if (liType == LineItemType.Disbursement) { amount *= -1; }
      this.amount = amount;
      Date = date;
      dateType = DateType.Date;
      Type = liType;
    }
    public LineItem(double amount, int periods, int oddDays, LineItemType liType)
    {
      if (amount < 0)
      {
        throw new ApplicationException("New Line Item must have an Amount >= 0");
      }
      if (liType == LineItemType.Disbursement) { amount *= -1; }
      this.amount = amount;
      this.span.Periods = periods;
      this.span.OddDays = oddDays;
      dateType = DateType.Periods;
      Type = liType;
    }
    #endregion

    #region "Properties"
    /// <summary>
    /// The Amount of the Line Item as a positive value
    /// </summary>
    public double Amount
    {
      get => amount * (int)Type;
      set => amount = value;
    }

    /// <summary>
    /// The Date of the Line Item
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The Recurrence of Periods after the first Line Item.  Usually
    /// An integer representing the number of recurring payments.
    /// </summary>
    public int Periods
    {
      get => span.Periods;
      set => span.Periods = value;
    }

    /// <summary>
    /// The Number of Odd Days off the Common Period.  ONLY set this 
    /// if you are sure of the number of days off the common period
    /// for this line item per Regulation Z.  If you are unsure, set the 
    /// date instead!!!
    /// </summary>
    public int OddDays
    {
      get => span.OddDays;
      set => span.OddDays = value;
    }

    /// <summary>
    /// The Type of Line Item this is:  Payment or Disbursement
    /// </summary>
    public LineItemType Type { get; set; }

    /// <summary>
    /// The Period Type for recurrence.  
    /// </summary>
    public UnitPeriod RecurrencePeriod { get; set; }

    /// <summary>
    /// The Present Value Interest Factor for this Line Item
    /// </summary>
    public double PresentValueInterestFactor { get; set; }

    /// <summary>
    /// The Present Value of the Line Item
    /// </summary>
    public double PresentValue { get; set; }

    /// <summary>
    /// The Balance of the Loan as of this Line Item
    /// </summary>
    public double Balance { get; private set; }

    /// <summary>
    /// The Parent Collection for this Line Item
    /// </summary>
    public LineItemCollection Parent { get; set; }

    /// <summary>
    /// The Number of times this line item repeats for the given Recurrence Period
    /// </summary>
    public int NumberOccurrences { get; set; }

    /// <summary>
    /// The Current Active APR Rate
    /// </summary>
    public double ActiveRate { get; private set; }

    #endregion

    #region "Methods"
    /// <summary>
    /// Mark the Line Item As Complete.  Must only be done by the Line Item Collection
    /// </summary>
    internal void Complete()
    {
      if (this.dateType == DateType.Date)
      {
        this.span = DateTimeCalculations.GetNumberPeriods(this.Parent.StartDate, this.Date, this.Parent.CommonPeriod);
      }
      else
      {
        if (this.Parent.CommonPeriod.NumPeriods < 1 || this.Parent.CommonPeriod.NumPeriods < 1)
        {
          throw new ApplicationException("Cannot mark LineItem for completion.  A period has been specified, but there is no common period for the Parent LineItemCollection Class");
        }
        this.Date = DateTimeCalculations.GetDateFromPeriod(this.span, this.Parent.StartDate, this.Parent.CommonPeriod);
      }
    }

    /// <summary>
    /// Sets the Active Interest Rate
    /// </summary>
    public void SetActiveRate(double activeRate, double priorBalance)
    {
      this.ActiveRate = activeRate;
      if (this.NumberOccurrences > 1)
      {
        this.PresentValueInterestFactor = FinancialCalculations.GetPresentValueInterestFactorAnnuity(this.Parent.StartDate, this.Date,
                                                        this.RecurrencePeriod, this.ActiveRate,
                                                        this.Parent.PeriodsPerYear, this.Parent.DaysPerPeriod,
                                                        this.NumberOccurrences, this.Parent.CommonPeriod);
      }
      else
      {
        this.PresentValueInterestFactor = FinancialCalculations.GetPresentValueInterestFactor(this.span, this.ActiveRate, this.Parent.PeriodsPerYear, this.Parent.DaysPerPeriod);
      }

      this.PresentValue = this.amount * this.PresentValueInterestFactor;
      this.Balance = priorBalance + this.PresentValue;
    }

    /// <summary>
    /// Pulls an XmlNode from the LineItem
    /// </summary>
    public string ToJson()
    {
      return System.Text.Json.JsonSerializer.Serialize(this);
    }


    #endregion
  }
}
