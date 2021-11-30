using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
                this._moveChangeAmountInfo = moveChangeAmountInfo;
                this._rotateChangeAmountInfo = rotateChangeAmountInfo;
            }

            public void Do()
            {
                foreach (Studio.GuideCommand.EqualsInfo info in this._moveChangeAmountInfo)
                {
                    Studio.ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.pos = info.newValue;
                }
                foreach (Studio.GuideCommand.EqualsInfo info in this._rotateChangeAmountInfo)
                {
                    Studio.ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.rot = info.newValue;
                }
            }

            public void Redo()
            {
                this.Do();
            }

            public void Undo()
            {
                foreach (Studio.GuideCommand.EqualsInfo info in this._moveChangeAmountInfo)
                {
                    Studio.ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.pos = info.oldValue;
                }
                foreach (Studio.GuideCommand.EqualsInfo info in this._rotateChangeAmountInfo)
                {
                    Studio.ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.rot = info.oldValue;
                }
            }
        }
    }
}
