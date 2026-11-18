using System.Collections.Generic;

namespace DataChampions
{
    public static class Skills
    {
        public const int Pictogram = 0, BarChart = 1, Venn = 2, Carroll = 3, Count = 4;
        public static readonly string[] Names = { "Pictograms", "Bar charts", "Venn diagrams", "Carroll diagrams" };
        public static readonly string[] Tags = { "PICTOGRAM", "BAR CHART", "VENN DIAGRAM", "CARROLL DIAGRAM" };
        public static readonly string[] Icons = { "mini_picto", "mini_bar", "mini_venn", "mini_carroll" };
    }

    public enum DiagramId { Cookies, Vegetables, VennNumbers, BallsBar, Trees, SeaAnimals, CarrollNumbers, CarrollNames, CarrollShapes }

    public enum StepKind { Highlight, Count, ReadBar, Caption }

    /// <summary>One beat of an animated explanation (or hint) played over the diagram.</summary>
    public struct ExplainStep
    {
        public StepKind kind;
        public string target;   // region or item group
        public int step;        // counting increment (2 when a picture stands for 2)
        public bool keepCount;  // carry on counting from the previous group
        public string text;

        public static ExplainStep Highlight(string region) => new ExplainStep { kind = StepKind.Highlight, target = region };
        public static ExplainStep Count(string group, int step = 1, bool keep = false) =>
            new ExplainStep { kind = StepKind.Count, target = group, step = step, keepCount = keep };
        public static ExplainStep ReadBar(int bar) => new ExplainStep { kind = StepKind.ReadBar, target = bar.ToString() };
        public static ExplainStep Caption(string text) => new ExplainStep { kind = StepKind.Caption, text = text };
    }

    public class QuestionDef
    {
        public int id;               // 1..10, also the voice line keys q#, h#, e#
        public DiagramId diagram;
        public int skill;
        public int points;
        public string text;
        public string[] options;
        public int answer;           // index into options
        public string hint;
        public string[] hintRegions;
        public ExplainStep[] explain;
        public string unit;          // "students", "trees"... shown with the answer

        public string QuestionVoice => "q" + id;
        public string HintVoice => "h" + id;
        public string ExplainVoice => "e" + id;
        public string AnswerText => options[answer];

        public static string[] Num(params int[] v) => System.Array.ConvertAll(v, x => x.ToString());
    }

    /// <summary>
    /// The ten questions, exactly the ones in the lesson's reference images (Cambridge Stage 4, Unit 4 Lesson 2:
    /// graphs, Venn and Carroll diagrams). British spelling; each wrong option is a common slip
    /// (forgetting the key, skipping the overlap, counting the wrong box...).
    /// </summary>
    public static class QuestionBank
    {
        public static readonly List<QuestionDef> All = new List<QuestionDef>
        {
            new QuestionDef
            {
                id = 1, diagram = DiagramId.Cookies, skill = Skills.Pictogram, points = 300,
                text = "How many students voted in the survey?",
                options = QuestionDef.Num(14, 24, 26, 28, 30, 32), answer = 3, unit = "students",
                hint = "Look at the key. Each cookie stands for 2 students.",
                hintRegions = new[] { "key" },
                explain = new[]
                {
                    ExplainStep.Highlight("key"),
                    ExplainStep.Count("cookies", 2),
                    ExplainStep.Caption("14 cookies × 2 = 28 students"),
                }
            },
            new QuestionDef
            {
                id = 2, diagram = DiagramId.Vegetables, skill = Skills.Carroll, points = 100,
                text = "How many vegetables are NOT green?",
                options = QuestionDef.Num(2, 3, 4, 5, 6, 8), answer = 2, unit = "vegetables",
                hint = "Only look in the 'Not green vegetables' column.",
                hintRegions = new[] { "colNot" },
                explain = new[]
                {
                    ExplainStep.Highlight("colNot"),
                    ExplainStep.Count("notgreen"),
                    ExplainStep.Caption("4 vegetables are not green"),
                }
            },
            new QuestionDef
            {
                id = 3, diagram = DiagramId.VennNumbers, skill = Skills.Venn, points = 200,
                text = "How many numbers are odd?",
                options = QuestionDef.Num(5, 8, 9, 10, 11, 15), answer = 3, unit = "numbers",
                hint = "Don't forget the numbers where the two circles overlap!",
                hintRegions = new[] { "leftCircle" },
                explain = new[]
                {
                    ExplainStep.Highlight("leftCircle"),
                    ExplainStep.Count("leftOnly"),
                    ExplainStep.Count("both", 1, true),
                    ExplainStep.Caption("5 + 5 = 10 odd numbers"),
                }
            },
            new QuestionDef
            {
                id = 4, diagram = DiagramId.BallsBar, skill = Skills.BarChart, points = 100,
                text = "How many beach balls and volleyballs are there altogether?",
                // 7 = forgot the scale goes up in 5s (counted lines), 30/40 = one line off, 45 = beach + basket, 15 = difference
                options = QuestionDef.Num(7, 15, 30, 35, 40, 45), answer = 3, unit = "balls",
                hint = "The numbers go up in fives. Read across from the top of each bar.",
                hintRegions = new[] { "axis" },
                explain = new[]
                {
                    ExplainStep.ReadBar(0),
                    ExplainStep.ReadBar(1),
                    ExplainStep.Caption("25 + 10 = 35 balls altogether"),
                }
            },
            new QuestionDef
            {
                id = 5, diagram = DiagramId.Trees, skill = Skills.Pictogram, points = 200,
                text = "Each tree picture stands for 2 trees. How many trees were planted on Saturday?",
                options = QuestionDef.Num(2, 3, 4, 5, 6, 8), answer = 2, unit = "trees",
                hint = "Find Saturday. Remember, each picture means 2 trees.",
                hintRegions = new[] { "row5" },
                explain = new[]
                {
                    ExplainStep.Highlight("row5"),
                    ExplainStep.Count("row5", 2),
                    ExplainStep.Caption("2 pictures × 2 = 4 trees"),
                }
            },
            new QuestionDef
            {
                id = 6, diagram = DiagramId.SeaAnimals, skill = Skills.Pictogram, points = 100,
                text = "How many sea animals are there altogether?",
                options = QuestionDef.Num(4, 10, 11, 12, 13, 14), answer = 3, unit = "sea animals",
                hint = "Count every animal, in every row.",
                hintRegions = new[] { "table" },
                explain = new[]
                {
                    ExplainStep.Highlight("table"),
                    ExplainStep.Count("all"),
                    ExplainStep.Caption("4 + 1 + 5 + 2 = 12 sea animals"),
                }
            },
            new QuestionDef
            {
                id = 7, diagram = DiagramId.VennNumbers, skill = Skills.Venn, points = 200,
                text = "How many numbers are greater than 10?",
                options = QuestionDef.Num(5, 9, 10, 11, 15, 20), answer = 2, unit = "numbers",
                hint = "Look inside the whole right-hand circle, including the overlap.",
                hintRegions = new[] { "rightCircle" },
                explain = new[]
                {
                    ExplainStep.Highlight("rightCircle"),
                    ExplainStep.Count("both"),
                    ExplainStep.Count("rightOnly", 1, true),
                    ExplainStep.Caption("5 + 5 = 10 numbers greater than 10"),
                }
            },
            new QuestionDef
            {
                id = 8, diagram = DiagramId.CarrollNumbers, skill = Skills.Carroll, points = 300,
                text = "Which numbers are multiples of 3 and even numbers?",
                options = new[] { "6, 18, 24", "9, 21, 27", "10, 20, 40", "25, 35, 55", "6, 9, 21", "6, 10, 20" }, answer = 0, unit = "numbers",
                hint = "Find where the 'multiple of 3' row meets the 'even number' column.",
                hintRegions = new[] { "row0", "col0" },
                explain = new[]
                {
                    ExplainStep.Highlight("row0"),
                    ExplainStep.Highlight("col0"),
                    ExplainStep.Highlight("cell00"),
                    ExplainStep.Count("cell00"),
                    ExplainStep.Caption("6, 18 and 24 are multiples of 3 and even"),
                }
            },
            new QuestionDef
            {
                id = 9, diagram = DiagramId.CarrollNames, skill = Skills.Carroll, points = 200,
                text = "How many children can ride a bike and swim?",
                options = QuestionDef.Num(3, 4, 5, 6, 7, 13), answer = 1, unit = "children",
                hint = "Find the box where 'Can swim' meets 'Can ride a bike'.",
                hintRegions = new[] { "row0", "col0" },
                explain = new[]
                {
                    ExplainStep.Highlight("row0"),
                    ExplainStep.Highlight("col0"),
                    ExplainStep.Highlight("cell00"),
                    ExplainStep.Count("cell00"),
                    ExplainStep.Caption("Lily, Olivia, Jack and Tom: 4 children"),
                }
            },
            new QuestionDef
            {
                id = 10, diagram = DiagramId.CarrollShapes, skill = Skills.Carroll, points = 300,
                text = "How many shapes are symmetrical and have more than 4 corners?",
                options = QuestionDef.Num(1, 2, 3, 4, 5, 6), answer = 1, unit = "shapes",
                hint = "Find where the 'Symmetrical' row meets the 'More than 4 corners' column.",
                hintRegions = new[] { "row0", "col1" },
                explain = new[]
                {
                    ExplainStep.Highlight("row0"),
                    ExplainStep.Highlight("col1"),
                    ExplainStep.Highlight("cell01"),
                    ExplainStep.Count("cell01"),
                    ExplainStep.Caption("A star and a pentagon: 2 shapes"),
                }
            },
        };

        public static QuestionDef ById(int id) => All.Find(q => q.id == id);
    }

    public class BadgeDef
    {
        public string id, title, description, icon;
    }

    public static class Badges
    {
        public static readonly List<BadgeDef> All = new List<BadgeDef>
        {
            new BadgeDef { id = "pictogram_pro", title = "Pictogram Pro", description = "All 3 pictograms right in one game", icon = "fl_framed_picture" },
            new BadgeDef { id = "venn_wizard", title = "Venn Wizard", description = "Both Venn diagrams right in one game", icon = "fl_magic_wand" },
            new BadgeDef { id = "carroll_champ", title = "Carroll Champ", description = "All 4 Carroll diagrams right in one game", icon = "fl_crown" },
            new BadgeDef { id = "daredevil", title = "Daredevil", description = "Win 3 risks in one game", icon = "fl_fire" },
            new BadgeDef { id = "hot_streak", title = "Hot Streak", description = "5 right answers in a row", icon = "fl_high_voltage" },
            new BadgeDef { id = "perfect_ten", title = "Perfect Ten", description = "All 10 questions right", icon = "fl_hundred_points" },
        };

        public static BadgeDef ById(string id) => All.Find(b => b.id == id);
    }
}
