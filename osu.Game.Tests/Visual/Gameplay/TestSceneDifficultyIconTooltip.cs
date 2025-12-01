// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;

namespace osu.Game.Tests.Visual.Gameplay
{
    [HeadlessTest]
    public partial class TestSceneDifficultyIconTooltip : OsuTestScene
    {

        public const double BASE_STARS = 5.55;

        private BeatmapSetInfo importedSet;

        private TestBeatmapDifficultyCache difficultyCache;

        private IBindable<StarDifficulty> starDifficultyBindable;


        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("setup difficulty cache", () =>
            {
                Beatmap.Value = CreateWorkingBeatmap(Ruleset.Value);

                importedSet = Beatmap.Value.BeatmapSetInfo;

                SelectedMods.Value = Array.Empty<Mod>();

                Child = difficultyCache = new TestBeatmapDifficultyCache();

                starDifficultyBindable = difficultyCache.GetBindableDifficulty(importedSet.Beatmaps.First());
            });

            AddUntilStep($"star difficulty -> {BASE_STARS}", () => starDifficultyBindable.Value.Stars == BASE_STARS);
        }

        [Test]
        [TestCase(DifficultyIconTooltipType.None, false)]
        [TestCase(DifficultyIconTooltipType.StarRating, true)]
        [TestCase(DifficultyIconTooltipType.Extended, true)]
        public void TestCreateTooltipContent(DifficultyIconTooltipType type, bool expected)
        {
            bool success = false;
            AddStep("create tooltip content", () =>
            {
                try
                {
                    DifficultyIconTooltipContent ct =
                        new DifficultyIconTooltipContent(
                            Beatmap.Value.BeatmapInfo,
                            starDifficultyBindable,
                            Ruleset.Value,
                            null,
                            type);
                }
                catch
                {
                    success = false;
                    return;
                }
                success = true;
            });
            AddAssert("created without crashing", () => success == expected);
        }


        [Test]
        public void TestSetContentUpdatesFields()
        {
            DifficultyIconTooltip tooltip = null!;

            AddStep("create tooltip", () =>
            {
                tooltip = new DifficultyIconTooltip();
                Child = tooltip;
            });

            DifficultyIconTooltipContent content = null!;

            AddStep("set content", () =>
            {
                content = new DifficultyIconTooltipContent(
                    Beatmap.Value.BeatmapInfo,
                    starDifficultyBindable,
                    Ruleset.Value,
                    null,
                    DifficultyIconTooltipType.Extended);

                tooltip.SetContent(content);
            });

            AddAssert("difficulty name matches", () =>
                tooltip.ChildrenOfType<OsuSpriteText>().First().Text
                    == Beatmap.Value.BeatmapInfo.DifficultyName);

            AddAssert("attributes container visible", () =>
                tooltip.ChildrenOfType<FillFlowContainer>()
                       .First(f => f.Alpha > 0) != null);
        }

        [Test]
        public void TestRebindingStarRating()
        {
            DifficultyIconTooltip tooltip = null!;
            var secondBindable = new Bindable<StarDifficulty>(new StarDifficulty(7.77, 0));

            AddStep("create tooltip", () =>
            {
                tooltip = new DifficultyIconTooltip();
                Child = tooltip;
            });

            AddStep("set first content", () =>
            {
                tooltip.SetContent(new DifficultyIconTooltipContent(
                    Beatmap.Value.BeatmapInfo,
                    starDifficultyBindable,
                    Ruleset.Value,
                    null,
                    DifficultyIconTooltipType.StarRating));
            });

            AddStep("set second content", () =>
            {
                tooltip.SetContent(new DifficultyIconTooltipContent(
                    Beatmap.Value.BeatmapInfo,
                    secondBindable,
                    Ruleset.Value,
                    null,
                    DifficultyIconTooltipType.StarRating));
            });

            AddStep("change second bindable", () =>
            {
                secondBindable.Value = new StarDifficulty(8.12, 0);
            });

            AddAssert("star rating display updated", () =>
                tooltip.ChildrenOfType<StarRatingDisplay>().First().Current.Value.Stars == 8.12);
        }

        [Test]
        public void TestTooltipTypeVisibility()
        {
            DifficultyIconTooltip tooltip = null!;
            AddStep("create tooltip", () => Child = tooltip = new DifficultyIconTooltip());

            AddStep("set star rating type", () =>
            {
                tooltip.SetContent(new DifficultyIconTooltipContent(
                    Beatmap.Value.BeatmapInfo,
                    starDifficultyBindable,
                    Ruleset.Value,
                    null,
                    DifficultyIconTooltipType.StarRating));
            });

            AddAssert("containers hidden", () =>
            {
                var containers = tooltip.ChildrenOfType<FillFlowContainer>().ToArray();
                return containers[1].Alpha == 0 && containers[2].Alpha == 0;
            });

            AddStep("set extended type", () =>
            {
                tooltip.SetContent(new DifficultyIconTooltipContent(
                    Beatmap.Value.BeatmapInfo,
                    starDifficultyBindable,
                    Ruleset.Value,
                    null,
                    DifficultyIconTooltipType.Extended));
            });

            AddAssert("containers visible", () =>
            {
                var containers = tooltip.ChildrenOfType<FillFlowContainer>().ToArray();
                return containers[1].Alpha == 1 && containers[2].Alpha == 1;
            });
        }

        [Test]
        public void TestBPMAndLengthWithRateMods()
        {
            DifficultyIconTooltip tooltip = null!;
            AddStep("create tooltip", () => Child = tooltip = new DifficultyIconTooltip());

            AddStep("set content with rate mod", () =>
            {
                OsuRuleset ruleset = new OsuRuleset();
                var mods = new Mod[] { ruleset.CreateMod<ModDoubleTime>() };

                tooltip.SetContent(new DifficultyIconTooltipContent(
                    Beatmap.Value.BeatmapInfo,
                    starDifficultyBindable,
                    Ruleset.Value,
                    mods,
                    DifficultyIconTooltipType.Extended));
            });

            AddAssert("bpm adjusted", () =>
                tooltip.ChildrenOfType<OsuSpriteText>()
                       .Any(t => t.Text.ToString().Contains("BPM: "))
            );
        }

        [Test]
        public void TestAttributeListPopulated()
        {
            DifficultyIconTooltip tooltip = null!;
            AddStep("create tooltip", () => Child = tooltip = new DifficultyIconTooltip());

            AddStep("set extended content", () =>
            {
                tooltip.SetContent(new DifficultyIconTooltipContent(
                    Beatmap.Value.BeatmapInfo,
                    starDifficultyBindable,
                    Ruleset.Value,
                    null,
                    DifficultyIconTooltipType.Extended));
            });

            AddAssert("has attributes", () =>
                tooltip
                    .ChildrenOfType<FillFlowContainer>()
                    .Skip(1) // first is outer container, second is attributes container
                    .First()
                    .ChildrenOfType<OsuSpriteText>()
                    .Any());
        }

        [Test]
        public void TestShowHideAnimation()
        {
            DifficultyIconTooltip tooltip = null!;
            AddStep("create tooltip", () => Child = tooltip = new DifficultyIconTooltip());

            AddStep("hide", () => tooltip.Hide());
            AddUntilStep("hidden", () => tooltip.Alpha == 0);

            AddStep("show", () => tooltip.Show());
            AddUntilStep("visible", () => tooltip.Alpha == 1);
        }

        private partial class TestBeatmapDifficultyCache : BeatmapDifficultyCache
        {
            public Func<DifficultyCacheLookup, StarDifficulty> ComputeDifficulty { get; set; }

            protected override Task<StarDifficulty?> ComputeValueAsync(DifficultyCacheLookup lookup, CancellationToken token = default)
            {
                return Task.FromResult<StarDifficulty?>(ComputeDifficulty?.Invoke(lookup) ?? new StarDifficulty(BASE_STARS, 0));
            }
        }
    }
}
