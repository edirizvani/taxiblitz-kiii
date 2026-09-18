using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Tests.Infrastructure;

public class TourBuilder
{
    private string _title = "Test Tour";
    private string _description = "A test tour description that is long enough.";
    private decimal _price = 100m;
    private string _duration = "4 hours";
    private string _startingPoint = "Ohrid";
    private string _endingPoint = "Struga";
    private string? _routeStopsText;

    public TourBuilder WithTitle(string t) { _title = t; return this; }
    public TourBuilder WithPrice(decimal p) { _price = p; return this; }
    public TourBuilder WithDuration(string d) { _duration = d; return this; }
    public TourBuilder WithStartingPoint(string s) { _startingPoint = s; return this; }
    public TourBuilder WithEndingPoint(string e) { _endingPoint = e; return this; }
    public TourBuilder WithRoute(string r) { _routeStopsText = r; return this; }
    public TourBuilder AsDeleted() { _title = "Deleted"; return this; }

    public Tour Build() => new Tour
    {
        Title             = _title,
        Description       = _description,
        Price             = _price,
        Duration          = _duration,
        StartingPoint     = _startingPoint,
        EndingPoint       = _endingPoint,
        RouteStopsText    = _routeStopsText ?? "",
        CulturalHighlights = "",
        PhotoProfileUrl   = "",
        YouTubeLink       = ""
    };
}

public class BookingBuilder
{
    private string _name = "Test Booker";
    private string _email = "customer@test.com";
    private string _phone = "+38976111111";
    private int _people = 2;
    private DateTime _dateTime = DateTime.Now.AddDays(1);
    private string _status = "Pending";
    private int _tourId = 1;
    private int? _driverId;
    private decimal? _discount;
    private int? _referralCodeId;

    public BookingBuilder WithName(string n) { _name = n; return this; }
    public BookingBuilder WithCustomerEmail(string e) { _email = e; return this; }
    public BookingBuilder WithPhone(string p) { _phone = p; return this; }
    public BookingBuilder WithPeople(int c) { _people = c; return this; }
    public BookingBuilder WithDateTime(DateTime dt) { _dateTime = dt; return this; }
    public BookingBuilder WithStatus(string s) { _status = s; return this; }
    public BookingBuilder WithTourId(int id) { _tourId = id; return this; }
    public BookingBuilder WithDriverId(int? id) { _driverId = id; return this; }
    public BookingBuilder WithDiscount(decimal d) { _discount = d; return this; }
    public BookingBuilder WithReferral(int id) { _referralCodeId = id; return this; }

    public BookingTour Build() => new BookingTour
    {
        NameOfBookMaker = _name,
        CustomerEmail   = _email,
        PhoneNumber     = _phone,
        NumberOfPeople  = _people,
        BookingDateTime = _dateTime,
        Status          = _status,
        TourId          = _tourId,
        DriverId        = _driverId,
        DiscountAmount  = _discount,
        ReferralCodeId  = _referralCodeId
    };
}

public class DriverBuilder
{
    private string _name = "Test Driver";
    private string _email = "driver@test.com";
    private double? _rating;

    public DriverBuilder WithName(string n) { _name = n; return this; }
    public DriverBuilder WithEmail(string e) { _email = e; return this; }
    public DriverBuilder WithRating(double r) { _rating = r; return this; }
    public DriverBuilder AsPlaceholder() { _name = "Doesn't matter"; return this; }

    public Driver Build() => new Driver { Name = _name, Email = _email, Rating = _rating };
}

public class ReviewBuilder
{
    private string _reviewer = "Reviewer";
    private string _text = "Great tour!";
    private int? _rating;
    private DateTime _date = DateTime.Now;

    public ReviewBuilder WithRating(int? r) { _rating = r; return this; }
    public ReviewBuilder WithDate(DateTime d) { _date = d; return this; }
    public ReviewBuilder WithReviewer(string n) { _reviewer = n; return this; }
    public ReviewBuilder WithText(string t) { _text = t; return this; }

    public Review Build() => new Review
    {
        ReviewerName = _reviewer,
        Text         = _text,
        Rating       = _rating,
        ReviewDate   = _date
    };
}

public class TourPostBuilder
{
    private string _title = "Test Story";
    private string _excerpt = "Short excerpt here for testing.";
    private string _content = "Full content of the story goes here for testing purposes.";
    private bool _published;
    private bool _featured;
    private string? _tags;
    private string? _destination;
    private DateTime _created = DateTime.Now;

    public TourPostBuilder AsPublished() { _published = true; return this; }
    public TourPostBuilder AsFeatured() { _featured = true; _published = true; return this; }
    public TourPostBuilder WithTags(string t) { _tags = t; return this; }
    public TourPostBuilder WithDestination(string d) { _destination = d; return this; }
    public TourPostBuilder WithTitle(string t) { _title = t; return this; }
    public TourPostBuilder WithCreatedDate(DateTime d) { _created = d; return this; }

    public TourPost Build() => new TourPost
    {
        Title          = _title,
        Excerpt        = _excerpt,
        Content        = _content,
        IsPublished    = _published,
        IsFeatured     = _featured,
        Tags           = _tags,
        TourDestination = _destination,
        CreatedDate    = _created
    };
}

public class ReferralCodeBuilder
{
    private string _code = "TB1234";
    private string _ownerId = "owner1";
    private bool _active = true;
    private decimal _discount = 5m;

    public ReferralCodeBuilder WithCode(string c) { _code = c; return this; }
    public ReferralCodeBuilder WithOwner(string o) { _ownerId = o; return this; }
    public ReferralCodeBuilder AsInactive() { _active = false; return this; }
    public ReferralCodeBuilder WithDiscount(decimal d) { _discount = d; return this; }

    public ReferralCode Build() => new ReferralCode
    {
        Code           = _code,
        OwnerId        = _ownerId,
        IsActive       = _active,
        DiscountPercent = _discount
    };
}

public class ApplicationUserBuilder
{
    private string _firstName = "John";
    private string _lastName = "Doe";
    private string _phone = "+38976000000";
    private string _email = "user@test.com";
    private bool _emailConfirmed = true;

    public ApplicationUserBuilder WithFirstName(string f) { _firstName = f; return this; }
    public ApplicationUserBuilder WithLastName(string l) { _lastName = l; return this; }
    public ApplicationUserBuilder WithPhone(string p) { _phone = p; return this; }
    public ApplicationUserBuilder WithEmail(string e) { _email = e; return this; }
    public ApplicationUserBuilder AsIncomplete() { _firstName = ""; _lastName = ""; _phone = ""; return this; }
    public ApplicationUserBuilder WithUnconfirmedEmail() { _emailConfirmed = false; return this; }

    public ApplicationUser Build() => new ApplicationUser
    {
        UserName       = _email,
        Email          = _email,
        EmailConfirmed = _emailConfirmed,
        FirstName      = _firstName,
        LastName       = _lastName,
        PhoneNumber    = _phone
    };
}
