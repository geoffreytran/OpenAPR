using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace OpenAPR
{
  public sealed class Apr
  {
    private readonly ILogger<Apr> logger;

    public Apr(ILogger<Apr> logger, LineItemCollection lineItems)
    {
      this.LineItems = lineItems ?? throw new ArgumentNullException(nameof(lineItems));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// The Calculated APR
    /// </summary>
    public double CalculatedAprCalc { get; private set; } = 0.0d;

    /// <summary>
    /// A LineItems Item Collection
    /// </summary>
    public LineItemCollection LineItems { get; private set; }

    /// <summary>
    /// The Unit Period Type
    /// </summary>
    public UnitPeriod UnitPeriod
    {
      get => this.LineItems.CommonPeriod;
      set => this.LineItems.CommonPeriod = value;
    }

    /// <summary>
    /// The Entire APR Output as Json
    /// </summary>
    public string ToJson()
    {
      return System.Text.Json.JsonSerializer.Serialize(this);
    }

    /// <summary>
    /// Calculate the APR passing in the interest rate as a starting point
    /// </summary>
    public double Calculate(double startingRate)
    {
      return calculateAPR(startingRate);
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
      var sb = new StringBuilder();
      sb.Append("<APRIterations>");
      this.CalculatedAprCalc = rate;
      if (this.LineItems == null)
      {
        throw new ApplicationException("LineItems must be input first");
      }
      if (!this.LineItems.Completed)
      {
        this.LineItems.MarkComplete();
      }
      var iterations = 0;
      var lastBalance = 0.0d;
      var precision = 0.1d;
      this.LineItems.Sort();

      do
      {
        //Set the active rate and iterate through all the line items, ultimately
        //getting a final balance
        this.LineItems.SetActiveRate(CalculatedAprCalc);
        sb.Append($"<Iteration Item=\"{iterations}\">");
        sb.Append($"<Precision>{precision}</Precision>");
        sb.Append($"<Rate>{CalculatedAprCalc}</Rate>");
        sb.Append($"<FinalBalance>{this.LineItems.FinalBalance}</FinalBalance>");
        sb.Append("</Iteration>");
        iterations++;
        //If the last balance was on the other side of the Zero from this one
        //then we need to get more precise...
        if ((lastBalance < 0 && this.LineItems.FinalBalance > 0) ||
                (lastBalance > 0 && this.LineItems.FinalBalance < 0))
        {
          precision /= 10;
        }

        if ((lastBalance < 0 && precision > 0) ||
                (lastBalance > 0 && precision < 0))
        {
          precision *= -1;
        }

        CalculatedAprCalc += precision;

        lastBalance = this.LineItems.FinalBalance;
      } while ((this.LineItems.FinalBalance > 0.001 || this.LineItems.FinalBalance < -0.001) && iterations < 1000);

      logger.LogDebug("APRCalc Diagnostics: {0}", sb);
      Console.WriteLine(sb);
      return CalculatedAprCalc;
    }
  }
}
