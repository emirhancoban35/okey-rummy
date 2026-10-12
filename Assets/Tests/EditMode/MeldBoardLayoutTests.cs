using System.Linq;
using NUnit.Framework;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;
namespace Okey.Tests.EditMode
{
    public sealed class MeldBoardLayoutTests
    {
        private static readonly OkeyRules Rules = new OkeyRules(4,21,1,13,101,5,101,202,101,-101,101,2,2);
        private static readonly MeldRules Validation = new MeldRules(Rules,Tile(99,13,StoneColor.Black));
        private static StoneInstance Tile(int id,int value,StoneColor color=StoneColor.Red) =>
            new StoneInstance(id,new StoneData(StoneType.Regular,color,value));
        private static Meld Run(params int[] values)
        {
            Assert.That(Validation.TryCreate(values.Select((value,index)=>Tile(index,value)),MeldKind.Run,out var meld),Is.True);
            return meld;
        }
        [Test]
        public void RunsReserveSpaceForEveryPossibleValueOnBothEnds()
        {
            var layout=new MeldBoardLayout(8,2,1,13);
            var low=Run(1,2,3);var middle=Run(3,4,5);
            Assert.That(layout.Locate(low,0,0).Column,Is.Zero);
            Assert.That(layout.Locate(middle,1,0).Column,Is.EqualTo(2));
            Assert.That(layout.Locate(middle,1,2).Column,Is.EqualTo(4));
            Assert.That(layout.Locate(middle,1,0).Row,Is.EqualTo(1));
            Assert.That(layout.SeriesColumns,Is.EqualTo(13));
        }
        [Test]
        public void ExtendingEitherEndKeepsEveryExistingTileAndOtherMeldInItsCell()
        {
            var layout=new MeldBoardLayout(8,2,1,13);var run=Run(3,4,5);var other=Run(9,10,11);
            var original=run.Tiles.Select((tile,index)=>layout.Locate(run,0,index)).ToArray();
            Validation.TryExtend(run,Tile(10,2),out var left);
            Validation.TryExtend(left,Tile(11,6),out var both);
            for(var index=0;index<run.Tiles.Count;index++)
            {
                var tileIndex=both.Tiles.ToList().FindIndex(tile=>tile.Stone.Id==run.Tiles[index].Stone.Id);
                Assert.That(layout.Locate(both,0,tileIndex),Is.EqualTo(original[index]));
            }
            Assert.That(layout.Locate(other,1,0),Is.EqualTo(new MeldBoardCell(0,0,1,8)));
        }
        [Test]
        public void RowsFillDownwardBeforeAnotherLaneAndPage()
        {
            var layout=new MeldBoardLayout(8,2,1,13);var run=Run(3,4,5);
            Assert.That(layout.Locate(run,7,0),Is.EqualTo(new MeldBoardCell(0,0,7,2)));
            Assert.That(layout.Locate(run,8,0),Is.EqualTo(new MeldBoardCell(0,1,0,2)));
            Assert.That(layout.Locate(run,16,0),Is.EqualTo(new MeldBoardCell(1,0,0,2)));
            Assert.That(layout.PageCount(32),Is.EqualTo(2));
        }
        [Test]
        public void ThirtyTwoMeldsHaveDistinctRowsAndStayInsideTheirLanes()
        {
            var layout=new MeldBoardLayout(8,2,1,13);var run=Run(11,12,13);
            var cells=Enumerable.Range(0,32).Select(index=>layout.Locate(run,index,0)).ToArray();
            Assert.That(cells.Distinct().Count(),Is.EqualTo(32));
            Assert.That(cells.All(cell=>cell.Page<2 && cell.Lane<2 && cell.Row<8 && cell.Column<13),Is.True);
        }
        [Test]
        public void SetsStayContiguousAndAppendTheirFourthTileWithoutMovingExistingTiles()
        {
            var stones=new[]{Tile(0,6),Tile(1,6,StoneColor.Yellow),Tile(2,6,StoneColor.Black)};
            Validation.TryCreate(stones,MeldKind.Set,out var meld);var layout=new MeldBoardLayout(8,2,1,13);
            Validation.TryExtend(meld,Tile(3,6,StoneColor.Blue),out var extended);
            Assert.That(layout.Locate(extended,0,3).Column,Is.EqualTo(3));
            Assert.That(Enumerable.Range(0,3).Select(index => layout.Locate(meld,0,index).Column), Is.EqualTo(new[] { 0,1,2 }));
            Assert.That(layout.Locate(extended,0,1).Column,Is.EqualTo(layout.Locate(meld,0,1).Column));
        }
    }
}
