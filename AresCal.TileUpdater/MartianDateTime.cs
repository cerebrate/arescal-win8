using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;

namespace ArkaneSystems.AresCal.TileUpdater
{
    public sealed class MartianDateTime
    {
        private readonly DateTimeOffset unixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);

        #region Constructors
        public MartianDateTime()
            : this(DateTimeOffset.Now.ToUniversalTime())
        {}

        public MartianDateTime(DateTimeOffset date)
        {
            // The parameter is a DateTimeOffset because WinRT hates DateTimes.

            // Compute time data, based on supplied DateTimeOffset.
            // Get the Julian date (Earthside).
            double modifiedJulianDate = ((date - this.unixEpoch).TotalSeconds / 86400) + 2440587.5 - 2400000.5;

            // Compute from this the Martian sol date.
            this.MartianSolDate = (modifiedJulianDate - 51549.0) / 1.02749125 + 44795.9998;

            // subtract epoch to get elapsed time since.
            double elapsed = this.MartianSolDate - 143.00708;

            // Convert elapsed time to a count of sols and seconds.
            this.TotalSols = Convert.ToInt32(Math.Round(elapsed - 0.5, MidpointRounding.AwayFromZero));
            int seconds = Convert.ToInt32(Math.Round(
                (elapsed - Math.Round(elapsed - 0.5, MidpointRounding.AwayFromZero)) * 88775,
                MidpointRounding.AwayFromZero));

            // Compute time portion.
            int minutes = seconds / 60;
            seconds = seconds % 60;

            int hours = minutes / 60;
            minutes = minutes % 60;

            // TODO: Time Zone Adjustment

            // Detect gap.
            this.IsInGap = (hours == 24);

            // Make time strings.
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

            if ((roamingSettings.Values.ContainsKey("tsFreezeDuringGap")) &&
                ((bool) roamingSettings.Values["tsFreezeDuringGap"] == true) &&
                this.IsInGap)
            {
                this.Time = "24:00:00";
                this.ShortTime = "24:00";
            }
            else
            {
                this.Time = string.Format("{0:d}:{1:d2}:{2:d2}", hours, minutes, seconds);
                this.ShortTime = string.Format("{0:d}:{1:d2}", hours, minutes);
            }

            // Compute the date.
            int sols = this.TotalSols;

            // First, let's pull out the decades, and add the 141 years for the epoch.
            int annos = ((sols / 6686) * 10) + 141;
            sols = sols % 6686;

            // Start chopping years and leap years.  Note that at this point, there cannot be more
            // than nine years and some spare sols in here.
            while (true)
            {
                // Act differently depending on current year.
                if ((annos % 2 == 1) || (annos % 10 == 0))
                {
                    // Odd-numbered or 10-divisible leap year.)
                    if (sols < 669)
                        break;
                    else
                    {
                        annos++;
                        sols -= 669;
                    }
                }
                else
                {
                    // Even-numbered non-leap year.
                    if (sols < 668)
                        break;
                    else
                    {
                        annos++;
                        sols -= 668;
                    }
                }
            }

            // Convert sols remaining to day number.
            int day = sols + 1;

            // Look up the month names.
            var monthnames = this.GetMonthNameFromDay(ref day);

            this.DayOfWeek = day % 7;

            string weeksol = this.GetSolOfWeek(this.DayOfWeek);
            string weekday = this.GetDayOfWeek(this.DayOfWeek);

            this.Date = String.Format("{0}, {1:d} {2} {3:d}", weeksol, annos, monthnames.Item1, day);
            this.EclipsePhaseDate = String.Format("{0}, {1:d} {2} {3:d}", weekday, annos, monthnames.Item2, day);
        }
        #endregion

        public double MartianSolDate { get; private set; }

        private int TotalSols { get; set; }

        public bool IsInGap { get; private set; }

        public string Time { get; private set; }

        public string ShortTime { get; private set; }

        public string Date { get; private set; }

        public int DayOfWeek { get; private set; }

        public string EclipsePhaseDate { get; private set; }

        public string TranshumanSpaceDate
        {
            get
            {
                int tsSols = this.TotalSols;

                // Do special computations for Transhuman Space date.
                // Do epoch compensation.  Epoch for TS Calendar is 1 Virgo, 221 (Chinese mars landing).
                // However, we move epoch two decades back (1 Virgo, 201) and compensate in the annos, since
                // it makes our calculations easier.
                tsSols = tsSols - 40617;

                // Pull out the decades.
                int tsAnnos = ((tsSols / 6686) * 10) + (-10);
                tsSols = tsSols % 6686;

                // Chop years and leap years.  This is, believe it or not, an irregular pattern:
                // years ending 1, 3 6, 8 aren't, years ending 2, 4, 5, 7, 9, 0 are.
                while (true)
                {
                    int daysInSol;

                    switch (tsAnnos % 10)
                    {
                        case 1:
                        case 3:
                        case 6:
                        case 8:
                            daysInSol = 668;
                            break;

                        case 2:
                        case 4:
                        case 5:
                        case 7:
                        case 9:
                        case 0:
                            daysInSol = 669;
                            break;

                        default:
                            daysInSol = 669;
                            break;
                    }

                    if (tsSols < daysInSol)
                        break;
                    else
                    {
                        tsAnnos++;
                        tsSols -= daysInSol;
                    }
                }

                // Perform the epoch compensation on annos.
                tsAnnos -= 20;

                // Convert sols remaining to day number.
                int tsDay = tsSols + 1;

                string tsMonth = this.GetTsMonthFromDay(ref tsDay);
                string tsWeekday = this.GetDayOfWeek(tsDay % 7);

                return String.Format("{0}, {1:d} {2}, m{3:d4}", tsWeekday, tsDay, tsMonth, tsAnnos);
            }
        }

        private Tuple<string, string> GetMonthNameFromDay(ref int day)
        {
            if (day < 29)
            {
                return new Tuple<string, string>(@"Sagittarius", @"March");
            }
            else if (day < 57)
            {
                day -= 28;
                return new Tuple<string, string>(@"Dhanus", @"Dhanus");
            }
            else if (day < 85)
            {
                day -= 56;
                return new Tuple<string, string>(@"Capricornus", @"April");
            }
            else if (day < 113)
            {
                day -= 84;
                return new Tuple<string, string>(@"Makara", @"Makara");
            }
            else if (day < 141)
            {
                day -= 112;
                return new Tuple<string, string>(@"Aquarius", @"May");
            }
            else if (day < 168)
            {
                day -= 140;
                return new Tuple<string, string>(@"Kumbha", @"Kumbha");
            }
            else if (day < 196)
            {
                day -= 167;
                return new Tuple<string, string>(@"Pisces", @"June");
            }
            else if (day < 224)
            {
                day -= 195;
                return new Tuple<string, string>(@"Mina", @"Mina");
            }
            else if (day < 252)
            {
                day -= 223;
                return new Tuple<string, string>(@"Aries", @"July");
            }
            else if (day < 280)
            {
                day -= 251;
                return new Tuple<string, string>(@"Mesha", @"Mesha");
            }
            else if (day < 308)
            {
                day -= 279;
                return new Tuple<string, string>(@"Taurus", @"August");
            }
            else if (day < 335)
            {
                day -= 307;
                return new Tuple<string, string>(@"Rishabha", @"Rishabha");
            }
            else if (day < 363)
            {
                day -= 334;
                return new Tuple<string, string>(@"Gemini", @"September");
            }
            else if (day < 391)
            {
                day -= 362;
                return new Tuple<string, string>(@"Mithuna", @"Mithuna");
            }
            else if (day < 419)
            {
                day -= 390;
                return new Tuple<string, string>(@"Cancer", @"October");
            }
            else if (day < 447)
            {
                day -= 418;
                return new Tuple<string, string>(@"Karka", @"Karka");
            }
            else if (day < 475)
            {
                day -= 446;
                return new Tuple<string, string>(@"Leo", @"November");
            }
            else if (day < 502)
            {
                day -= 474;
                return new Tuple<string, string>(@"Simha", @"Simha");
            }
            else if (day < 530)
            {
                day -= 501;
                return new Tuple<string, string>(@"Virgo", @"December");
            }
            else if (day < 558)
            {
                day -= 529;
                return new Tuple<string, string>(@"Kanya", @"Kanya");
            }
            else if (day < 586)
            {
                day -= 557;
                return new Tuple<string, string>(@"Libra", @"January");
            }
            else if (day < 614)
            {
                day -= 585;
                return new Tuple<string, string>(@"Tula", @"Tula");
            }
            else if (day < 642)
            {
                day -= 613;
                return new Tuple<string, string>(@"Scorpius", @"February");
            }
            else
            {
                day -= 641;
                return new Tuple<string, string>(@"Vrishika", @"Vrishika");
            }
        }

        private string GetSolOfWeek(int dow)
        {
            switch (dow)
            {
                // In encounter order for results of the modulus operation.
                case 1:
                    return @"Sol Solis";

                case 2:
                    return @"Sol Lunae";

                case 3:
                    return @"Sol Martius";

                case 4:
                    return @"Sol Mercurii";

                case 5:
                    return @"Sol Jovis";

                case 6:
                    return @"Sol Veneris";

                case 0:
                    return @"Sol Saturni";

                default:
                    throw new InvalidOperationException();
            }
        }

        private string GetDayOfWeek(int dow)
        {
            switch (dow)
            {
                // In encounter order for results of the modulus operation.

                case 1:
                    return @"Sunday";

                case 2:
                    return @"Monday";

                case 3:
                    return @"Tuesday";

                case 4:
                    return @"Wednesday";

                case 5:
                    return @"Thursday";

                case 6:
                    return @"Friday";

                case 0:
                    return @"Saturday";

                default:
                    throw new InvalidOperationException();
            }
        }

        private string GetTsMonthFromDay(ref int day)
        {
            string tsmonth;

            if (day < 29)
            {
                tsmonth = @"January";
            }
            else if (day < 57)
            {
                tsmonth = @"Virgo";
                day -= 28;
            }
            else if (day < 85)
            {
                tsmonth = @"February";
                day -= 56;
            }
            else if (day < 113)
            {
                tsmonth = @"Libra";
                day -= 84;
            }
            else if (day < 141)
            {
                tsmonth = @"March";
                day -= 112;
            }
            else if (day < 168)
            {
                tsmonth = @"Scorpius";
                day -= 140;
            }
            else if (day < 196)
            {
                tsmonth = @"April";
                day -= 167;
            }
            else if (day < 224)
            {
                tsmonth = @"Sagittarius";
                day -= 195;
            }
            else if (day < 252)
            {
                tsmonth = @"May";
                day -= 223;
            }
            else if (day < 280)
            {
                tsmonth = @"Capricornus";
                day -= 251;
            }
            else if (day < 308)
            {
                tsmonth = @"June";
                day -= 279;
            }
            else if (day < 335)
            {
                tsmonth = @"Aquarius";
                day -= 307;
            }
            else if (day < 363)
            {
                tsmonth = @"July";
                day -= 334;
            }
            else if (day < 391)
            {
                tsmonth = @"Pisces";
                day -= 362;
            }
            else if (day < 419)
            {
                tsmonth = @"August";
                day -= 390;
            }
            else if (day < 447)
            {
                tsmonth = @"Aries";
                day -= 418;
            }
            else if (day < 475)
            {
                tsmonth = @"September";
                day -= 446;
            }
            else if (day < 502)
            {
                tsmonth = @"Taurus";
                day -= 474;
            }
            else if (day < 530)
            {
                tsmonth = @"October";
                day -= 501;
            }
            else if (day < 558)
            {
                tsmonth = @"Gemini";
                day -= 529;
            }
            else if (day < 586)
            {
                tsmonth = @"November";
                day -= 557;
            }
            else if (day < 614)
            {
                tsmonth = @"Cancer";
                day -= 585;
            }
            else if (day < 642)
            {
                tsmonth = @"December";
                day -= 613;
            }
            else
            {
                tsmonth = @"Leo";
                day -= 641;
            }

            return tsmonth;
        }
    }
}
