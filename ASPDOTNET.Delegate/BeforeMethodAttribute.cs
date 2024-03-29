﻿using System;

namespace ASPDOTNET.Delegate
{
    public class BeforeMethodAttribute:AbstractMethodAttribute
    {
        public override Action doSomething(Action action)
        {
            //Console.WriteLine("do something before method");
            Action actionRes=new Action(() =>
            {
                action.Invoke();
            });
            return actionRes;
        }

        public override void beforeMethod()
        {
            Console.WriteLine(beforeMsg);
        }

        public override void afterMethod()
        {
            Console.WriteLine(afterMsg);
        }

        public BeforeMethodAttribute(string beforeMsg, string afterMsg)
        {
            this.beforeMsg = beforeMsg;
            this.afterMsg = afterMsg;
        }
    }
}