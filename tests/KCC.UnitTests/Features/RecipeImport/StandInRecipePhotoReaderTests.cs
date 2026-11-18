using System.Text.Json;
using KCC.Web.Features.DevTools.RecipeImport;
using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using TUnit.Assertions.Enums;

namespace KCC.UnitTests.Features.RecipeImport;

public class StandInRecipePhotoReaderTests
{
    private static readonly ReadPhoto Photo = new("image/jpeg", [0xFF, 0xD8, 0xFF, 0xE0]);

    [Test]
    public async Task AtTheDefaultDelay_ItAnswersAtOnce()
    {
        var read = Reader().ReadAsync(Request(), CancellationToken.None);

        _ = await Assert.That(read.IsCompletedSuccessfully).IsTrue();
        _ = await Assert.That((await read).Failure).IsNull();
    }

    [Test]
    public async Task WithADelaySet_ItAnswersOnlyOnceTheDelayHasPassed()
    {
        var clock = new ManualClock();
        var reader = Reader(options: new RecipeImportOptions { StandInDelaySeconds = 70 }, clock: clock);

        var read = reader.ReadAsync(Request(), CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(69));
        var answeredBeforeTheDelay = await Task.WhenAny(read, Task.Delay(TimeSpan.FromMilliseconds(200))) == read;
        clock.Advance(TimeSpan.FromSeconds(1));
        var attempt = await read.WaitAsync(TimeSpan.FromSeconds(10));

        _ = await Assert.That(answeredBeforeTheDelay).IsFalse();
        _ = await Assert.That(attempt.Failure).IsNull();
    }

    [Test]
    public async Task ItIsAlwaysConfigured()
    {
        var reader = Reader();

        _ = await Assert.That(reader.IsConfigured).IsTrue();
    }

    [Test]
    [Arguments(ImportFlows.Recipe)]
    [Arguments(ImportFlows.Variant)]
    public async Task ItsRecipe_HasAnEstimatedPrepTime_AnUnclearIngredient_AndAWrittenDescription(string flow)
    {
        var read = await NormalizedReadAsync(flow);

        _ = await Assert.That(read.Variant.PrepTime.Value).IsNotNull();
        _ = await Assert.That(read.Variant.PrepTime.Mark?.Kind).IsEqualTo(MarkKinds.Estimate);
        _ = await Assert.That(read.Ingredients.Count(ingredient => ingredient.Mark?.Kind == MarkKinds.Unclear)).IsEqualTo(1);
        _ = await Assert.That(read.Recipe.Description.Mark?.Kind).IsEqualTo(MarkKinds.Written);
        _ = await Assert.That(read.Variant.Description.Mark?.Kind).IsEqualTo(MarkKinds.Written);
    }

    [Test]
    [Arguments(ImportFlows.Recipe)]
    [Arguments(ImportFlows.Variant)]
    public async Task ItsRecipe_FillsEveryValueTheReviewRequires(string flow)
    {
        var read = await NormalizedReadAsync(flow);

        _ = await Assert.That(read.Outcome).IsEqualTo(ReadOutcomes.Recipe);
        _ = await Assert.That(read.Recipe.Name.Value).IsNotEmpty();
        _ = await Assert.That(read.Recipe.Description.Value).IsNotEmpty();
        _ = await Assert.That(read.Variant.Name.Value).IsNotEmpty();
        _ = await Assert.That(read.Ingredients.Count(ingredient => ingredient.Name.Length > 0)).IsGreaterThan(0);
        _ = await Assert.That(read.Instructions.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task ItsRead_CostsNothing()
    {
        var options = new RecipeImportOptions();

        var attempt = await Reader(options: options).ReadAsync(Request(), CancellationToken.None);

        _ = await Assert.That(attempt.Usage).IsNotNull();
        _ = await Assert.That(ReadCost.MicroUsd(ModelProfiles.Find(options.Model), attempt.Usage)).IsEqualTo(0L);
    }

    [Test]
    public async Task WithTwoPhotos_InTheRecipeFlow_ItProposesTheCatalogsR1()
    {
        var catalog = RecipeCatalog.From([Page("Weeknight Tacos", Shoyu()), Page("Avocado Toast", Toast())]);

        var attempt = await Reader().ReadAsync(Request(ImportFlows.Recipe, photos: 2, catalog), CancellationToken.None);

        _ = await Assert.That(attempt.Output.MatchId).IsEqualTo("R1");
        _ = await Assert.That(catalog.Match(attempt.Output.MatchId, attempt.Output.MatchReason)?.RecipeName).IsEqualTo("Avocado Toast");
        _ = await Assert.That(string.IsNullOrWhiteSpace(attempt.Output.MatchReason)).IsFalse();
    }

    [Test]
    [Arguments(1)]
    [Arguments(3)]
    [Arguments(4)]
    public async Task WithAnyOtherNumberOfPhotos_ItProposesNoRecipe(int photos)
    {
        var catalog = RecipeCatalog.From([Page("Avocado Toast", Toast())]);

        var attempt = await Reader().ReadAsync(Request(ImportFlows.Recipe, photos, catalog), CancellationToken.None);

        _ = await Assert.That(attempt.Output.MatchId).IsNull();
        _ = await Assert.That(attempt.Output.MatchReason).IsNull();
    }

    [Test]
    public async Task WithTwoPhotos_AndAnEmptyCatalog_ItProposesNoRecipe()
    {
        var attempt = await Reader().ReadAsync(Request(ImportFlows.Recipe, photos: 2, RecipeCatalog.From([])), CancellationToken.None);

        _ = await Assert.That(attempt.Output.MatchId).IsNull();
    }

    [Test]
    public async Task WithThreePhotos_InTheRecipeFlow_ItsRecipeCopiesTheFirstVariantOfTheCatalogsR1()
    {
        var catalog = RecipeCatalog.From([Page("Weeknight Tacos", Shoyu()), Page("Avocado Toast", Toast(), Shoyu())]);

        var attempt = await Reader().ReadAsync(Request(ImportFlows.Recipe, photos: 3, catalog), CancellationToken.None);

        await AssertCopiesToastAsync(attempt.Output);
    }

    [Test]
    public async Task WithThreePhotos_InTheVariantFlow_ItsRecipeCopiesTheFirstVariantOfTheChosenRecipe()
    {
        var toastPage = Page("Avocado Toast", Toast(), Shoyu());
        var recipes = new Mock<IRecipeQueries>();
        recipes.Setup(queries => queries.GetPublishedRecipes()).Returns([Page("Weeknight Tacos", Shoyu()), toastPage]);
        var request = new ReadRequest(ImportFlows.Variant, toastPage.Recipe.Key, [Photo, Photo, Photo], null);

        var attempt = await Reader(recipes.Object).ReadAsync(request, CancellationToken.None);

        await AssertCopiesToastAsync(attempt.Output);
    }

    [Test]
    public async Task WithThreePhotos_InTheVariantFlow_ForARecipeWithNoVariant_ItReadsItsOwnRecipe()
    {
        var bare = Page("Bare Board");
        var recipes = new Mock<IRecipeQueries>();
        recipes.Setup(queries => queries.GetPublishedRecipes()).Returns([bare]);
        var request = new ReadRequest(ImportFlows.Variant, bare.Recipe.Key, [Photo, Photo, Photo], null);

        var attempt = await Reader(recipes.Object).ReadAsync(request, CancellationToken.None);

        _ = await Assert.That(attempt.Output.Ingredients.Any(ingredient => ingredient.Name == "Cornmeal")).IsTrue();
    }

    [Test]
    public async Task WithOnePhoto_TheDetailsAreTheCategorySuggestedAndTheTagsAndPrintedPanel()
    {
        var read = await DetailsReadAsync(photos: 1);

        _ = await Assert.That(read.Details.Category.Value).IsEqualTo(Breakfast.Key);
        _ = await Assert.That(read.Details.Category.Mark?.Kind).IsEqualTo(MarkKinds.Suggested);
        _ = await Assert.That(read.Details.Category.Mark?.Reason).IsNotEmpty();
        _ = await Assert.That(read.Details.Tags.Select(tag => tag.Key)).IsEquivalentTo([Vegan.Key, Easy.Key], CollectionOrdering.Any);
        _ = await Assert.That(read.Details.Tags.Single(tag => tag.Key == Vegan.Key).Mark).IsNull();
        _ = await Assert.That(read.Details.Tags.Single(tag => tag.Key == Easy.Key).Mark?.Kind).IsEqualTo(MarkKinds.Suggested);
        _ = await Assert.That(read.Details.Difficulty.Value).IsEqualTo("Easy");
        _ = await Assert.That(read.Details.Difficulty.Mark).IsNull();
        _ = await Assert.That(read.Details.Nutrition.Calories.Value).IsEqualTo(320);
        _ = await Assert.That(read.Details.Nutrition.Calories.Mark).IsNull();
        _ = await Assert.That(read.Details.Nutrition.FiberG.Value).IsEqualTo(2);
        _ = await Assert.That(read.Details.Nutrition.FiberG.Mark?.Kind).IsEqualTo(MarkKinds.Calculated);
        _ = await Assert.That(read.Details.Nutrition.FiberG.Mark?.Arg).IsEqualTo("1.5 g");
    }

    [Test]
    [Arguments(2)]
    [Arguments(3)]
    public async Task WithTwoOrThreePhotos_TheDetailsAreTheOnePhotoDetails(int photos)
    {
        var catalog = RecipeCatalog.From([Page("Weeknight Tacos", Shoyu()), Page("Avocado Toast", Toast())]);
        var one = (await ReadOutputAsync(photos: 1, catalog)).Details;

        var output = await ReadOutputAsync(photos, catalog);

        _ = await Assert.That(JsonSerializer.Serialize(output.Details)).IsEqualTo(JsonSerializer.Serialize(one));
        if (photos == 2)
        {
            _ = await Assert.That(output.MatchId).IsEqualTo("R1");
        }
        else
        {
            await AssertCopiesToastAsync(output);
        }
    }

    [Test]
    public async Task WithFourPhotos_TheWholeRecipePanelInKilojoulesAndSaltIsDividedByTheEightPrintedServings()
    {
        var read = await DetailsReadAsync(photos: 4);

        var nutrition = read.Details.Nutrition;
        _ = await Assert.That(read.Variant.Servings.Value).IsEqualTo(8);
        _ = await Assert.That(read.Variant.Servings.Mark).IsNull();
        _ = await Assert.That(nutrition.Calories.Value).IsEqualTo(250);
        _ = await Assert.That(nutrition.ProteinG.Value).IsEqualTo(10);
        _ = await Assert.That(nutrition.CarbsG.Value).IsEqualTo(20);
        _ = await Assert.That(nutrition.FatG.Value).IsEqualTo(8);
        _ = await Assert.That(nutrition.FiberG.Value).IsEqualTo(2);
        _ = await Assert.That(nutrition.SodiumMg.Value).IsEqualTo(1000);
        _ = await Assert.That(new[]
            {
                nutrition.Calories, nutrition.ProteinG, nutrition.CarbsG, nutrition.FatG, nutrition.FiberG, nutrition.SodiumMg,
            }.All(value => value.Value is not null && value.Mark?.Kind == MarkKinds.Calculated)).IsTrue();
    }

    [Test]
    public async Task WithFivePhotos_TheServingsAreAnEstimate_SoNutritionStaysEmpty()
    {
        var read = await DetailsReadAsync(photos: 5);

        _ = await Assert.That(read.Variant.Servings.Mark?.Kind).IsEqualTo(MarkKinds.Estimate);
        _ = await Assert.That(read.Details.Nutrition).IsEqualTo(NutritionConversion.Empty());
    }

    [Test]
    public async Task ANameTheListsLack_IsDropped()
    {
        var taxonomy = ReaderTaxonomy.From(new DetailsChoices([Breakfast], [Easy]), ImportFlows.Recipe);

        var read = await DetailsReadAsync(photos: 1, taxonomy);

        _ = await Assert.That(read.Details.Tags.Select(tag => tag.Key)).IsEquivalentTo([Easy.Key], CollectionOrdering.Any);
        var withoutCategory = ReaderTaxonomy.From(new DetailsChoices([], [Easy, Vegan]), ImportFlows.Recipe);
        _ = await Assert.That((await DetailsReadAsync(photos: 1, withoutCategory)).Details.Category.Value).IsNull();
    }

    private static readonly CategoryChoice Breakfast = new(Guid.NewGuid(), "Breakfast");
    private static readonly TagChoice Vegan = new(Guid.NewGuid(), "Vegan", TagKinds.Diet);
    private static readonly TagChoice Easy = new(Guid.NewGuid(), "Easy", TagKinds.Style);

    private static ReaderTaxonomy Taxonomy() =>
        ReaderTaxonomy.From(new DetailsChoices([Breakfast], [Vegan, Easy]), ImportFlows.Recipe);

    private static async Task<ReaderOutput> ReadOutputAsync(int photos, RecipeCatalog catalog)
    {
        var request = new ReadRequest(ImportFlows.Recipe, null, [.. Enumerable.Repeat(Photo, photos)], catalog, Taxonomy());
        return (await Reader().ReadAsync(request, CancellationToken.None)).Output;
    }

    private static async Task<ReadResponse> DetailsReadAsync(int photos, ReaderTaxonomy taxonomy = null)
    {
        taxonomy ??= Taxonomy();
        var request = new ReadRequest(ImportFlows.Recipe, null, [.. Enumerable.Repeat(Photo, photos)], null, taxonomy);
        var attempt = await Reader().ReadAsync(request, CancellationToken.None);
        return ReadResultNormalizer.Normalize(attempt.Output, taxonomy, ImportFlows.Recipe);
    }

    private static async Task<ReadResponse> NormalizedReadAsync(string flow)
    {
        var reader = Reader();
        var attempt = await reader.ReadAsync(Request(flow), CancellationToken.None);
        return ReadResultNormalizer.Normalize(attempt.Output);
    }

    private static StandInRecipePhotoReader Reader(
        IRecipeQueries recipes = null,
        RecipeImportOptions options = null,
        TimeProvider clock = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => recipes ?? Mock.Of<IRecipeQueries>());
        var current = options ?? new RecipeImportOptions();
        return new StandInRecipePhotoReader(
            Mock.Of<IOptionsMonitor<RecipeImportOptions>>(monitor => monitor.CurrentValue == current),
            clock ?? new ManualClock(),
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());
    }

    private static ReadRequest Request(string flow = ImportFlows.Recipe) =>
        new(flow, flow == ImportFlows.Variant ? Guid.NewGuid() : null, [Photo], null);

    private static ReadRequest Request(string flow, int photos, RecipeCatalog catalog) =>
        new(flow, null, [.. Enumerable.Repeat(Photo, photos)], catalog);

    private static async Task AssertCopiesToastAsync(ReaderOutput output)
    {
        _ = await Assert.That(output.MatchId).IsNull();
        _ = await Assert.That(output.Ingredients).IsEquivalentTo(
            [
                new ReaderIngredient("Sourdough", 2, "slices", false, null),
                new ReaderIngredient("Avocado", 1, "whole", false, null),
                new ReaderIngredient("Flaky salt", null, string.Empty, true, null),
            ],
            CollectionOrdering.Matching);
        _ = await Assert.That(output.Steps).IsEquivalentTo(
            [new ReaderStep("Toast the bread.", null), new ReaderStep("Smash the avocado on top.", null)],
            CollectionOrdering.Matching);
    }

    private static RecipePageData Page(string name, params VariantRecord[] variants) => new(
        new RecipeRecord(Guid.NewGuid(), name, null, $"{name}, for one test.", null, null, null, null, default),
        variants,
        null);

    private static VariantRecord Toast() => Variant(
        "The Basic",
        """[{"name":"Sourdough","quantity":2,"unit":"slices","isEyeballed":false},{"name":"Avocado","quantity":1,"unit":"whole","isEyeballed":false},{"name":"Flaky salt","quantity":null,"unit":"","isEyeballed":true}]""",
        """[{"step":1,"text":"Toast the bread."},{"step":2,"text":"Smash the avocado on top."}]""");

    private static VariantRecord Shoyu() => Variant(
        "Chili Oil Shoyu",
        """[{"name":"Ramen noodles","quantity":1,"unit":"pack","isEyeballed":false}]""",
        """[{"step":1,"text":"Boil the noodles."}]""");

    private static VariantRecord Variant(string name, string ingredientsJson, string instructionsJson) => new(
        Guid.NewGuid(),
        name,
        null,
        null,
        null,
        null,
        0,
        0,
        1,
        null,
        new NutritionRecord(null, null, null, null, null, null, null, null),
        [],
        ingredientsJson,
        instructionsJson,
        null,
        default);

    private sealed class ManualClock : TimeProvider
    {
        private readonly Lock gate = new();
        private readonly List<ManualTimer> timers = [];
        private TimeSpan elapsed;

        private TimeSpan Elapsed
        {
            get
            {
                lock (gate)
                {
                    return elapsed;
                }
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            lock (gate)
            {
                timers.Add(timer);
            }

            _ = timer.Change(dueTime, period);
            return timer;
        }

        public void Advance(TimeSpan time)
        {
            ManualTimer[] due;
            lock (gate)
            {
                elapsed += time;
                due = [.. timers.Where(timer => timer.DueAt <= elapsed)];
            }

            foreach (var timer in due)
            {
                timer.Fire();
            }
        }

        private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object state) : ITimer
        {
            public TimeSpan? DueAt { get; private set; }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                DueAt = dueTime == Timeout.InfiniteTimeSpan ? null : clock.Elapsed + dueTime;
                return true;
            }

            public void Fire()
            {
                DueAt = null;
                callback(state);
            }

            public void Dispose() => DueAt = null;

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
