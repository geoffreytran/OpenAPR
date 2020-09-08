using System;
using System.Collections.Generic;
using System.Text;

namespace APRCalculator
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
        double m_Amount = 0.0d;
        PeriodSpan m_Span;
        private LineItemType m_Type;
        private DateTime m_Date;
        private double m_PVIF;
        private double m_PresentValue;
        private double m_Balance;
        private UnitPeriod m_Recurrence;
        private DateType m_DateType;
        private LineItemCollection m_Parent;
        private int m_Occurences;
        private double m_ActiveRate;
        #endregion

        #region ".ctor"
        public LineItem(double Amount, DateTime Date, LineItemType liType)
        {
            if (Amount < 0)
            {
                throw new ApplicationException("New Line Item must have an Amount >= 0");
            }
            if (liType == LineItemType.Disbursement) { Amount = Amount * -1; }
            m_Amount = Amount;
            m_Date = Date;
            m_DateType = DateType.Date;
            m_Type = liType;
        }
        public LineItem(double Amount, int Periods, int OddDays, LineItemType liType)
        {
            if (Amount < 0)
            {
                throw new ApplicationException("New Line Item must have an Amount >= 0");
            }
            if (liType == LineItemType.Disbursement) { Amount = Amount * -1; }
            m_Amount = Amount;
            this.m_Span.Periods = Periods;
            this.m_Span.OddDays = OddDays;
            m_DateType = DateType.Periods;
            m_Type = liType;
        }
        #endregion

        #region "Properties"
        /// <summary>
        /// The Amount of the Line Item as a positive value
        /// </summary>
        public double Amount
        {
            get
            {
                return m_Amount * (int)m_Type;
            }
            set 
            {
                m_Amount = value;
            }
        }

        /// <summary>
        /// The Date of the Line Item
        /// </summary>
        public DateTime Date
        {
            get
            {
                return m_Date;
            }
            set
            {
                m_Date = value;
            }
        }

        /// <summary>
        /// The Recurrence of Periods after the first Line Item.  Usually
        /// An integer representing the number of recurring payments.
        /// </summary>
        public int Periods
        {
            get
            {
                return m_Span.Periods;
            }
            set
            {
                m_Span.Periods = value;
            }
        }

        /// <summary>
        /// The Number of Odd Days off the Common Period.  ONLY set this 
        /// if you are sure of the number of days off the common period
        /// for this line item per Regulation Z.  If you are unsure, set the 
        /// date instead!!!
        /// </summary>
        public int OddDays
        {
            get
            {
                return m_Span.OddDays;
            }
            set
            {
                m_Span.OddDays = value;
            }
        }

        /// <summary>
        /// The Type of Line Item this is:  Payment or Disbursement
        /// </summary>
        public LineItemType Type
        {
            get
            {
                return this.m_Type;
            }
            set
            {
                m_Type = value;
            }
        }

        /// <summary>
        /// The Period Type for recurrence.  
        /// </summary>
        public UnitPeriod RecurrencePeriod
        {
            get
            {
                return this.m_Recurrence;
            }
            set
            {
                this.m_Recurrence = value;
            }
        }

        /// <summary>
        /// The Present Value Interest Factor for this Line Item
        /// </summary>
        public double PVIF
        {
            get
            {
                return this.m_PVIF;
            }
            set
            {
                this.m_PVIF = value;
            }
        }

        /// <summary>
        /// The Present Value of the Line Item
        /// </summary>
        public double PresentValue
        {
            get
            {
                return this.m_PresentValue;
            }
            set
            {
                this.m_PresentValue = value;
            }
        }

        /// <summary>
        /// The Balance of the Loan as of this Line Item
        /// </summary>
        public double Balance
        {
            get
            {
                return this.m_Balance;
            }
            set
            {
                this.m_Balance = value;
            }
        }

        /// <summary>
        /// The Parent Collection for this Line Item
        /// </summary>
        public LineItemCollection Parent
        {
            get
            {
                return this.m_Parent;
            }
            set
            {
                this.m_Parent = value;
            }
        }

        /// <summary>
        /// The Number of times this line item repeats for the given Recurrence Period
        /// </summary>
        public int NumberOccurrences
        {
            get
            {
                return this.m_Occurences;
            }
            set
            {
                m_Occurences = value;
            }
        }

        /// <summary>
        /// The Current Active APR Rate
        /// </summary>
        public double ActiveRate
        {
            get
            {
                return this.m_ActiveRate;
            }
        }

#endregion

        #region "Methods"
        /// <summary>
        /// Mark the Line Item As Complete.  Must only be done by the Line Item Collection
        /// </summary>
        internal void Complete()
        {
            if (this.m_DateType == DateType.Date)
            {
                this.m_Span = DateTimeCalculations.GetNumberPeriods(this.Parent.StartDate, this.m_Date, this.Parent.CommonPeriod);
            }
            else
            {
                if (this.Parent.CommonPeriod.numPeriods < 1 || this.Parent.CommonPeriod.numPeriods < 1)
                {
                    throw new ApplicationException("Cannot mark LineItem for completion.  A period has been specified, but there is no common period for the Parent LineItemCollection Class");
                }
                this.m_Date = DateTimeCalculations.GetDateFromPeriod(this.m_Span, this.Parent.StartDate, this.Parent.CommonPeriod);
            }
        }

        /// <summary>
        /// Sets the Active Interest Rate
        /// </summary>
        public void SetActiveRate(double ActiveRate, double PriorBalance)
        {
            this.m_ActiveRate = ActiveRate;
            if(this.m_Occurences > 1)
            {
                this.m_PVIF = FinancialCalculations.getPVIFA(this.Parent.StartDate, this.m_Date,
                                                                this.m_Recurrence, this.m_ActiveRate,
                                                                this.Parent.PeriodsPerYear, this.Parent.DaysPerPeriod,
                                                                this.m_Occurences, this.Parent.CommonPeriod);
            }
            else
            {
                this.m_PVIF = FinancialCalculations.getPVIF(this.m_Span, this.m_ActiveRate, this.Parent.PeriodsPerYear, this.Parent.DaysPerPeriod);
            }
            
            this.m_PresentValue = this.m_Amount * this.m_PVIF;
            this.m_Balance = PriorBalance + this.m_PresentValue;
        }

        /// <summary>
        /// Pulls an XmlNode from the LineItem
        /// </summary>
        public String ToXml()
        {
            return "<LineItem>\n" +
                    "   <Date>" + this.m_Date.ToShortDateString() + "</Date>\n" +
                    "   <Amount>" + this.m_Amount.ToString() + "</Amount>\n" +
                    "   <UnitPeriods>" + this.m_Span.Periods.ToString() + "</UnitPeriods>\n" +
                    "   <OddDays>" + this.m_Span.OddDays.ToString() + "</OddDays>\n" +
                    "   <NumberOccurrences>" + this.m_Occurences.ToString() + "</NumberOccurrences>\n" + 
                    "   <Recurrence>" + DateTimeCalculations.getUnitPeriodString(this.m_Recurrence) + "</Recurrence>\n" +
                    "   <PVIF>" + this.m_PVIF.ToString() + "</PVIF>\n" +
                    "   <PresentValue>" + this.m_PresentValue.ToString() + "</PVIF>\n" +
                    "   <Balance>" + this.m_Balance.ToString() + "</Balance>\n" +
                   "</LineItem>";
        }

        
        #endregion
    }
}
