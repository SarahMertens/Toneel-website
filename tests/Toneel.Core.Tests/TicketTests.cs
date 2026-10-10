using Toneel.Core.Export;

namespace Toneel.Core.Tests;

public class TicketTests
{
    [Fact]
    public void Prints_date_location_and_name_with_the_surname_first()
    {
        var ticket = Ticket.For(Sample.Reservation("Lotte", "Vermeulen"));

        Assert.NotNull(ticket);
        Assert.Equal("24 januari 2026", ticket!.Date);
        Assert.Equal("Zaal Noord", ticket.Location);
        Assert.Equal("Vermeulen Lotte", ticket.Name);
        Assert.Equal("Vermeulen_Lotte-Ticket-Rare_Wereld.png", ticket.FileName);
    }

    [Fact]
    public void There_is_no_ticket_without_a_date()
    {
        Assert.Null(Ticket.For(Sample.Reservation(undated: true)));
    }

    [Theory]
    [InlineData("De Smet", "De_Smet")]
    [InlineData("  Van den Berg ", "Van_den_Berg")]
    [InlineData("a/b\\c:d*e?f\"g<h>i|j", "a_b_c_d_e_f_g_h_i_j")]
    [InlineData("..", "ticket")]
    [InlineData("", "ticket")]
    [InlineData("Zoë", "Zoë")]
    public void Makes_names_safe_to_use_in_a_file_name(string name, string expected)
    {
        Assert.Equal(expected, Ticket.FileSafe(name));
    }
}
