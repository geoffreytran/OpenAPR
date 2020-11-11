using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APRCalculator
{
    public class APR
    {
        LineItemCollection m_LineItems;
        StringBuilder sb;
        double m_APR = 0.0d;

        public APR(LineItemCollection LineItems)
        {
            this.m_LineItems = LineItems;
            sb = new StringBuilder();
        }

        /// <summary>
        /// The Calculated APR
        /// </summary>
        public double CalculatedAPR
        {
            get
            {
                return this.m_APR;
            }
        }

        /// <summary>
        /// A LineItems Item Collection
        /// </summary>
        public LineItemCollection LineItems
        {
            get
            {
                return this.m_LineItems;
            }
        }

        /// <summary>
        /// The Unit Period Type
        /// </summary>
        public UnitPeriod UnitPeriod
        {
            get
            {
                if(this.m_LineItems == null)
                {
                    throw new ApplicationException("Line Items Must be Set First");
                }
                return this.m_LineItems.CommonPeriod;
            }
            set
            {
                if(this.m_LineItems == null)
                {
                    throw new ApplicationException("Line Items Must be Set First");
                }
                this.LineItems.CommonPeriod = value;
            }
        }

        /// <summary>
        /// The Entire APR Output as XML
        /// </summary>
        public String ToXml()
        {
            return "<APR>\n" +
                " <CalculatedAPR>" + this.m_APR.ToString() + "</CalculatedAPR>" +
                m_LineItems.ToXml() +
                sb.ToString() +
                "</APR>";
        }

        /// <summary>
        /// Calculate the APR passing in the interest rate as a starting point
        /// </summary>
        public double Calculate(double StartingRate)
        {
            return calculateAPR(StartingRate);
        }

        /// <summary>
        /// Calculate the APR without a starting basis
        /// </summary>
        public double Calculate()
        {
            return calculateAPR(0);
        }

        private double calculateAPR(double rate)
        {
            sb.AppendLine("   <APRIterations>");
            this.m_APR = rate;
            if (this.m_LineItems == null)
            {
                throw new ApplicationException("LineItems must be input first");
            }
            if (!this.m_LineItems.Completed)
            {
                this.m_LineItems.MarkComplete();
            }
            int iIterations = 0;
            double dLastBalance = 0;
            double dPrecision = 0.1d;
            this.m_LineItems.Sort();

            do
            {
                //Set the active rate and iterate through all the line items, ultimately
                //getting a final balance
                this.m_LineItems.SetActiveRate(m_APR);
                sb.Append("     <Iteration Item=\"" + iIterations.ToString() + "\">");
                sb.Append("       <Precision>" + dPrecision.ToString() + "</Precision>");
                sb.Append("       <Rate>" + m_APR.ToString() + "</Rate>");
                sb.Append("       <FinalBalance>" + this.m_LineItems.FinalBalance.ToString() + "</FinalBalance>");
                sb.Append("     </Iteration>");
                iIterations++;
                //If the last balance was on the other side of the Zero from this one
                //then we need to get more precise...
                if ((dLastBalance < 0 && this.m_LineItems.FinalBalance > 0) ||
                        (dLastBalance > 0 && this.m_LineItems.FinalBalance < 0))
                {
                    dPrecision = dPrecision / 10;
                }

                if ((dLastBalance < 0 && dPrecision > 0) ||
                        (dLastBalance > 0 && dPrecision < 0))
                {
                    dPrecision = dPrecision * -1;
                }

                m_APR += dPrecision;

                dLastBalance = this.m_LineItems.FinalBalance;
            } while ((this.m_LineItems.FinalBalance > 0.001 || this.m_LineItems.FinalBalance < -0.001) && iIterations < 1000);
            return m_APR;
        }
    }
}
