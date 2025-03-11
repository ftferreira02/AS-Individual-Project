using System.Diagnostics.Metrics;

namespace eShop.Ordering.API.Telemetry
{
    public static class TelemetryMetrics
    {
        private static readonly Meter Meter = new("Ordering.API");


        private static readonly Counter<long> _orderPlacedCounter =
            Meter.CreateCounter<long>("order_placed_count", description: "Total number of placed orders.");

        private static readonly Counter<double> _totalRevenueCounter =
            Meter.CreateCounter<double>("total_revenue", description: "Total revenue from completed orders.");

        private static readonly Counter<long> _canceledOrdersCounter =
            Meter.CreateCounter<long>("canceled_orders", description: "Total number of canceled orders.");

        private static readonly UpDownCounter<int> _activeOrdersGauge =
            Meter.CreateUpDownCounter<int>("active_orders", description: "Number of currently active orders.");

        private static readonly Counter<long> _ordersByCountryCounter =
            Meter.CreateCounter<long>("orders_by_country", description: "Total number of placed orders by country.");

        private static readonly Counter<long> _itemsSoldCounter =
            Meter.CreateCounter<long>("items_sold", description: "Total number of individual items sold.");

        public static Counter<long> ItemsSoldCounter => _itemsSoldCounter;
        public static Counter<long> OrderPlacedCounter => _orderPlacedCounter;
        public static Counter<double> TotalRevenueCounter => _totalRevenueCounter;
        public static Counter<long> CanceledOrdersCounter => _canceledOrdersCounter;
        public static UpDownCounter<int> ActiveOrdersGauge => _activeOrdersGauge;
        public static Counter<long> OrdersByCountryCounter => _ordersByCountryCounter;

    }
}
