using BadCourt.SharedKernel;
using Shouldly;
using Xunit;

namespace BadCourt.UnitTests.SharedKernel;

public class PagedListTests
{
    [Fact]
    public void The_first_page_of_several_has_a_next_page_but_no_previous()
    {
        PagedList<string> page = new(["a", "b"], page: 1, pageSize: 2, totalCount: 5);

        page.HasPreviousPage.ShouldBeFalse();
        page.HasNextPage.ShouldBeTrue();
        page.Items.Count.ShouldBe(2);
    }

    [Fact]
    public void The_last_page_has_a_previous_page_but_no_next()
    {
        PagedList<string> page = new(["e"], page: 3, pageSize: 2, totalCount: 5);

        page.HasPreviousPage.ShouldBeTrue();
        page.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public void A_partial_final_page_is_still_a_page()
    {
        PagedList<string> page = new(["a"], page: 1, pageSize: 10, totalCount: 25);

        page.TotalPages.ShouldBe(3);
    }

    [Fact]
    public void An_empty_result_has_nowhere_to_go()
    {
        PagedList<string> page = new([], page: 1, pageSize: 10, totalCount: 0);

        page.TotalPages.ShouldBe(0);
        page.HasNextPage.ShouldBeFalse();
        page.HasPreviousPage.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    public void Paging_arguments_have_to_make_sense(int page, int pageSize)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new PagedList<string>([], page, pageSize, totalCount: 0));
    }
}
