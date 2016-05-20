namespace Org.Apache.CuratorNet.Client
{
  public enum TimeUnit
    {
        /**
         * Time unit representing one thousandth of a microsecond
         */
        Nanoseconds,
        /**
         * Time unit representing one thousandth of a millisecond
         */
        Microseconds,

        /**
         * Time unit representing one thousandth of a second
         */
        Milliseconds,

        /**
         * Time unit representing one second
         */
        Seconds,

        /**
         * Time unit representing sixty seconds
         */
        Minutes,

        /**
         * Time unit representing sixty minutes
         */
        Hours,

        /**
         * Time unit representing twenty four hours
         */
        Days
    }
}
