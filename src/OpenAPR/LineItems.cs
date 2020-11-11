using System;
using System.Collections.Generic;
using System.Text;

namespace APRCalculator
{
    public enum CollectionType
    {
        Date,
        Periods
    }

    public class LineItemCollection : IEnumerable<LineItem>
    {
        #region "Private Fields"
        private DateTime m_StartDate;
        private List<LineItem> m_Items;
        private UnitPeriod m_CommonPeriod;
        private bool m_Completed;
        private CollectionType m_CollType;
        private double m_PeriodsPerYear;
        private int m_DaysPerPeriod;
        private double m_FinalBalance;
        private double m_APR;
        #endregion

        public LineItemCollection(CollectionType Type)
        {
            this.m_Items = new List<LineItem>();
            this.m_CollType = Type;
        }

        /// <summary>
        /// The Date of the First Line Item
        /// </summary>
        public DateTime StartDate
        {
            get
            {
                return m_StartDate;
            }
            set
            {
                m_StartDate = value;
            }
        }

        /// <summary>
        /// The number of line items in the collection
        /// </summary>
        public int Count
        {
            get
            {
                return this.m_Items.Count;
            }
        }

        /// <summary>
        /// Indicates whether the collection has been marked as complete.  It must be marked as complete prior to using in the APR calculation
        /// </summary>
        public bool Completed
        {
            get
            {
                return this.m_Completed;
            }
        }

        /// <summary>
        /// Indicates whether the input line item dates are Dates, or number of periods and odd days after the first line item
        /// </summary>
        public CollectionType Type
        {
            get
            {
                return this.m_CollType;
            }
        }

        /// <summary>
        /// The CommonPeriod Type for this calculation.  
        /// ONLY set this if you are absolutely sure of the most frequent
        /// period per regulation Z.
        /// </summary>
        public UnitPeriod CommonPeriod
        {
            get
            {
                return m_CommonPeriod;
            }
            set
            {
                this.m_CommonPeriod = value;
            }
        }
        /// <summary>
        /// Add a Line item
        /// </summary>
        public void Add(LineItem li)
        {
            if (!this.m_Completed)
            {
                li.Parent = this;
                m_Items.Add(li);
            }
            else
            {
                ApplicationException newex = new ApplicationException("Cannot Add a LineItem after collection has been marked complete");
                newex.Source = "LineItemCollection.Add";
                throw newex;
            }
        }

        public LineItem this[int index]
        {
            get
            {
                return (LineItem)this.m_Items[index];
            }
            set
            {
                this.m_Items[index] = value;
            }
        }

        /// <summary>
        /// Finished Adding Line Items, must be used prior to adding to APR
        /// </summary>
        public void MarkComplete()
        {
            this.Sort();

            //And now we calculate the Unit Period
            this.m_CommonPeriod = DateTimeCalculations.CalculateCommonPeriod(this);
            this.m_DaysPerPeriod = DateTimeCalculations.DaysPerPeriod(this.m_CommonPeriod);
            this.m_PeriodsPerYear = DateTimeCalculations.PeriodsPerYear(this.m_CommonPeriod);

            //Mark all line items as complete
            for (int i = 0; i < this.m_Items.Count; i++)
            {
                LineItem li = (LineItem)this.m_Items[i];
                li.Complete();
            }
           
            m_Completed = true;
        }

        /// <summary>
        /// Sort the Line Items By Date
        /// </summary>
        internal void Sort()
        {
            for (int i = this.m_Items.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    LineItem li = (LineItem)this.m_Items[j];
                    LineItem liNext = (LineItem)this.m_Items[j + 1];
                    if (li.Date > liNext.Date)
                    {
                        LineItem temp = liNext;
                        this.m_Items[j + 1] = this.m_Items[j];
                        this.m_Items[j] = temp;
                    }
                }
            }
            this.m_StartDate = this.m_Items[0].Date;
        }

        /// <summary>
        /// Number of Unit Periods Per Year
        /// </summary>
        public double PeriodsPerYear
        {
            get
            {
                return this.m_PeriodsPerYear;
            }
        }

        /// <summary>
        /// Number of Days in each Unit Period
        /// </summary>
        public int DaysPerPeriod
        {
            get
            {
                return this.m_DaysPerPeriod;
            }
        }

        /// <summary>
        /// Set the ActiveRate for all line items
        /// </summary>
        public void SetActiveRate(double ActiveRate)
        {
            double RunningBalance = 0.0f;
            this.m_APR = ActiveRate;
            //Mark all line items as complete
            for (int i = 0; i < this.m_Items.Count; i++)
            {
                LineItem li = (LineItem)this.m_Items[i];
                li.SetActiveRate(ActiveRate, RunningBalance);
                RunningBalance = li.Balance;
            }
            //Set Final Balance based on the running balance for the last line item
            this.m_FinalBalance = RunningBalance;
        }

        public String ToXml()
        {
            System.Text.StringBuilder sb = new StringBuilder();
            sb.AppendLine("<LineItems>");
            sb.AppendLine("  <APR>" + this.m_APR.ToString() + "</APR>");
            sb.AppendLine("  <CommonPeriod>" + DateTimeCalculations.GetUnitPeriodString(this.m_CommonPeriod) + "</CommonPeriod>");
            sb.AppendLine("  <PeriodsPerYear>" + this.m_PeriodsPerYear.ToString() + "</PeriodsPerYear>");
            sb.AppendLine("  <DaysPerPeriod>" + this.m_DaysPerPeriod.ToString() + "</DaysPerPeriod>");
            sb.AppendLine("  <StartDate>" + this.m_StartDate.ToShortDateString() + "</StartDate>");
            sb.AppendLine("  <FinalBalance>" + this.m_FinalBalance.ToString() + "</FinalBalance>");
            for (int i = 0; i < this.m_Items.Count; i++)
            {
                LineItem li = (LineItem)this.m_Items[i];
                sb.Append(li.ToXml());
            }
            sb.AppendLine("</LineItems>");
            return sb.ToString();
        }

        /// <summary>
        /// The balance after all items have been amortized
        /// </summary>
        public double FinalBalance
        {
            get
            {
                return this.m_FinalBalance;
            }
        }

        #region IEnumerable<LineItem> Members

        public IEnumerator<LineItem> GetEnumerator()
        {
            return this.m_Items.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.m_Items.GetEnumerator();    
        }

        #endregion
    }
}
