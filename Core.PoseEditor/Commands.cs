namespace HSPE
{
    namespace Commands
    {
        public class MoveRotateEqualsCommand : Studio.ICommand
        {
            private readonly Studio.GuideCommand.EqualsInfo[] _moveChangeAmountInfo;
            private readonly Studio.GuideCommand.EqualsInfo[] _rotateChangeAmountInfo;

            public MoveRotateEqualsCommand(Studio.GuideCommand.EqualsInfo[] moveChangeAmountInfo, Studio.GuideCommand.EqualsInfo[] rotateChangeAmountInfo)
            {
                _moveChangeAmountInfo = moveChangeAmountInfo;
                _rotateChangeAmountInfo = rotateChangeAmountInfo;
            }

            public void Do()
            {
                foreach (Studio.GuideCommand.EqualsInfo info in _moveChangeAmountInfo)
                {
                    Studio.ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.pos = info.newValue;
                }
                foreach (Studio.GuideCommand.EqualsInfo info in _rotateChangeAmountInfo)
                {
                    Studio.ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.rot = info.newValue;
                }
            }

            public void Redo()
            {
                Do();
            }

            public void Undo()
            {
                foreach (Studio.GuideCommand.EqualsInfo info in _moveChangeAmountInfo)
                {
                    Studio.ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.pos = info.oldValue;
                }
                foreach (Studio.GuideCommand.EqualsInfo info in _rotateChangeAmountInfo)
                {
                    Studio.ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.rot = info.oldValue;
                }
            }
        }
    }
}
