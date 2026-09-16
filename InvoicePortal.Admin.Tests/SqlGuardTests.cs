using InvoicePortal.Admin.Ai;
using InvoicePortal.Admin.Ai.Query;

namespace InvoicePortal.Admin.Tests;

public class SqlGuardTests
{
    private static readonly object?[] NoParams = [];

    [Theory]
    [InlineData("SELECT TOP (200) Id, Number FROM Invoices WHERE IsDeleted = 0")]
    [InlineData("select b.Name, count(*) as N from Invoices i join Bottlers b on b.Id = i.BottlerId group by b.Name")]
    [InlineData("WITH t AS (SELECT BottlerId, SUM(Amount) AS Total FROM Invoices GROUP BY BottlerId) SELECT * FROM t")]
    [InlineData("  SELECT 1 AS One;  ")]
    [InlineData("SELECT Id, [From], [To] FROM Programs")]
    [InlineData("SELECT CreatedAt, UpdatedAt, IsDeleted FROM Invoices")]
    public void Accepts_single_select_statements(string sql)
    {
        var normalized = SqlGuard.Validate(sql, NoParams);
        Assert.DoesNotContain(";", normalized);
    }

    [Fact]
    public void Accepts_parameters_that_are_supplied_in_order()
    {
        var sql = "SELECT TOP (@p0) Name FROM Bottlers WHERE Name LIKE @p1";
        SqlGuard.Validate(sql, [5, "%coca%"]);
    }

    [Theory]
    [InlineData("DELETE FROM Invoices", "Only a single SELECT query is allowed.")]
    [InlineData("UPDATE Invoices SET Amount = 0", "Only a single SELECT query is allowed.")]
    [InlineData("SELECT 1; SELECT 2", "Only a single SELECT query is allowed.")]
    [InlineData("SELECT 1 -- comment", "Only a single SELECT query is allowed.")]
    [InlineData("SELECT /* x */ 1", "Only a single SELECT query is allowed.")]
    [InlineData("SELECT * INTO Backup FROM Invoices", "The query uses a disallowed keyword: INTO.")]
    [InlineData("SELECT 1 WHERE EXISTS (SELECT 1) EXEC sp_who", "The query uses a disallowed keyword: EXEC.")]
    [InlineData("SELECT * FROM OPENROWSET('x','y','z')", "The query uses a disallowed keyword: OPENROWSET.")]
    [InlineData("SELECT @@VERSION", "System variables such as @@VERSION are not allowed.")]
    public void Rejects_unsafe_statements(string sql, string expectedMessage)
    {
        var ex = Assert.Throws<QueryRejectedException>(() => SqlGuard.Validate(sql, NoParams));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_missing_sql(string? sql)
    {
        var ex = Assert.Throws<QueryRejectedException>(() => SqlGuard.Validate(sql, NoParams));
        Assert.Equal("Missing SQL command object.", ex.Message);
    }

    [Fact]
    public void Rejects_unknown_parameter_names()
    {
        var ex = Assert.Throws<QueryRejectedException>(() => SqlGuard.Validate("SELECT Name FROM Bottlers WHERE Name = @bottler", ["x"]));
        Assert.StartsWith("Unexpected parameter name @bottler", ex.Message);
    }

    [Fact]
    public void Rejects_parameters_that_are_referenced_but_not_supplied()
    {
        var ex = Assert.Throws<QueryRejectedException>(() => SqlGuard.Validate("SELECT Name FROM Bottlers WHERE Id = @p0 OR Id = @p1", [1]));
        Assert.Equal("Parameter @p1 is referenced but not supplied.", ex.Message);
    }

    [Fact]
    public void Rejects_too_many_parameters()
    {
        var values = Enumerable.Range(0, SqlGuard.MaxParameters + 1).Cast<object?>().ToArray();
        var ex = Assert.Throws<QueryRejectedException>(() => SqlGuard.Validate("SELECT 1 AS One", values));
        Assert.Equal("Too many parameters (max 50).", ex.Message);
    }

    [Fact]
    public void Rejects_non_primitive_parameters()
    {
        var ex = Assert.Throws<QueryRejectedException>(() => SqlGuard.Validate("SELECT 1 AS One", [new object()]));
        Assert.Equal("Only primitive parameter values are allowed.", ex.Message);
    }

    [Fact]
    public void Normalize_strips_a_single_trailing_semicolon()
    {
        Assert.Equal("SELECT 1", SqlGuard.Normalize("  SELECT 1;  "));
        Assert.Equal("SELECT 1;", SqlGuard.Normalize("SELECT 1;;"));
    }
}
