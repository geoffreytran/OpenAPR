using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenAPR
{
    public enum CollectionType
    {
        Date,
        Periods
    }

    public class LineItemCollection : IEnumerable<LineItem>
    {
        #region "Private Fields"
        private DateTime startDate;
        private readonly List<LineItem> lineItems;
        private double Apr;
        #endregion

        public LineItemCollection(CollectionType type)
        {
            this.lineItems = new List<LineItem>();
            this.Type = type;
        }

        /// <summary>
        /// The Date of the First Line Item
        /// </summary>
        public DateTime StartDate
        {
            get => startDate;
            set => startDate = value;
        }

        /// <summary>
        /// The number of line items in the collection
        /// </summary>
        public int Count => this.lineItems.Count;

        /// <summary>
        /// Indicates whether the collection has been marked as complete.  It must be marked as complete prior to using in the APR calculation
        /// </summary>
        public bool Completed { get; private set; }

        /// <summary>
        /// Indicates whether the input line item dates are Dates, or number of periods and odd days after the first line item
        /// </summary>
        public CollectionType Type { get; private set; }

        /// <summary>
        /// The CommonPeriod Type for this calculation.  
        /// ONLY set this if you are absolutely sure of the most frequent
        /// period per regulation Z.
        /// </summary>
        public UnitPeriod CommonPeriod { get; set; }

        /// <summary>
        /// Add a Line item
        /// </summary>
        public void Add(LineItem li)
        {
            if (!this.Completed)
            {
                li.Parent = this;
                lineItems.Add(li);
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
            get => (LineItem)this.lineItems[index];
            set => this.lineItems[index] = value;
        }

        /// <summary>
        /// Finished Adding Line Items, must be used prior to adding to APR
        /// </summary>
        public void MarkComplete()
        {
            this.Sort();

            //And now we calculate the Unit Period
            this.CommonPeriod = DateTimeCalculations.CalculateCommonPeriod(this);
            this.DaysPerPeriod = DateTimeCalculations.DaysPerPeriod(this.CommonPeriod);
            this.PeriodsPerYear = DateTimeCalculations.PeriodsPerYear(this.CommonPeriod);

            //Mark all line items as complete
            foreach (var li in this.lineItems.Cast<LineItem>())
            {
                li.Complete();
            }
           
            Completed = true;
        }

        /// <summary>
        /// Sort the Line Items By Date
        /// </summary>
        internal void Sort()
        {
            for (var i = this.lineItems.Count - 1; i >= 0; i--)
            {
                for (var j = 0; j < i; j++)
                {
                    var li = (LineItem)this.lineItems[j];
                    var liNext = (LineItem)this.lineItems[j + 1];
                    if (li.Date <= liNext.Date) continue;
                    var temp = liNext;
                    this.lineItems[j + 1] = this.lineItems[j];
                    this.lineItems[j] = temp;
                }
            }
            this.startDate = this.lineItems[0].Date;
        }

        /// <summary>
        /// Number of Unit Periods Per Year
        /// </summary>
        public double PeriodsPerYear { get; private set; }

        /// <summary>
        /// Number of Days in each Unit Period
        /// </summary>
        public int DaysPerPeriod { get; private set; }

        /// <summary>
        /// Set the ActiveRate for all line items
        /// </summary>
        public void SetActiveRate(double activeRate)
        {
            double runningBalance = 0.0f;
            this.Apr = activeRate;
            //Mark all line items as complete
            foreach (var li in this.lineItems)
            {
                li.SetActiveRate(activeRate, runningBalance);
                runningBalance = li.Balance;
            }
            //Set Final Balance based on the running balance for the last line item
            this.FinalBalance = runningBalance;
        }

        public string ToJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }

        /// <summary>
        /// The balance after all items have been amortized
        /// </summary>
        public double FinalBalance { get; private set; }

        #region IEnumerable<LineItem> Members

        public IEnumerator<LineItem> GetEnumerator()
        {
            return this.lineItems?.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.lineItems?.GetEnumerator();    
        }

        #endregion
    }
}
