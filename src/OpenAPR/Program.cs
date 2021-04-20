using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace OpenAPR
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("APR Calc");

            
            var lineItems = new LineItemCollection(CollectionType.Date);
            lineItems.CommonPeriod = new UnitPeriod {
                NumPeriods = 24,
                PeriodType = UnitPeriodType.Monthly
            };
            lineItems.Add(new LineItem(5000.00, new DateTime(1978, 01, 10), LineItemType.Disbursement));
            lineItems.Add(new LineItem(230.00, 24, 0, LineItemType.Payment));


            //             lineItems.Add(new LineItem(1000.00, new DateTime(2020, 11, 09), LineItemType.Disbursement));
            // lineItems.Add(new LineItem(290.57, new DateTime(2020, 12, 02), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2020, 12, 17), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2020, 12, 31), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2021, 01, 15), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2021, 02, 02), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2021, 02, 17), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2021, 03, 02), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2021, 03, 17), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2021, 04, 02), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2021, 04, 16), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2021, 04, 30), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2021, 05, 17), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2021, 06, 02), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, new DateTime(2021, 06, 17), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.54, new DateTime(2021, 07, 02), LineItemType.Payment));
            // lineItems.Add(new LineItem(290.57, 15, 0, LineItemType.Payment));


            ILoggerFactory factory = LoggerFactory.Create(builder => {
               builder.AddFilter("Microsoft", LogLevel.Debug);
               builder.AddFilter("System", LogLevel.Debug);
               builder.AddConsole();
            });
            var logger = factory.CreateLogger<Apr>();
            var aprCalc = new Apr(logger, lineItems);
            var apr = aprCalc.Calculate();
            Console.WriteLine($"APR: {apr}");

            return 1;
        }
    }
}
