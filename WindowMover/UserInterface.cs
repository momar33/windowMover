using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Collections;
using System.Windows.Forms;

namespace WindowMover
{
    class UserInterface
    {
        public static void MoveMinimizedWindowsToMainMonitor()
        {
            AutomationElement desktop = AutomationElement.RootElement;
            AutomationElementCollection taskbarButtons;
            AutomationElement leftTaskBar = null;

            leftTaskBar = GetLeftMonitorTaskBar();
            taskbarButtons = GetTaskbarButtons(leftTaskBar);

            foreach (AutomationElement el in taskbarButtons)
            {
                if (el.Current.Name != "")
                {
                    // Press the taskbar button to so that it moves to main monitor
                    InvokeAutomationElement(el);
                }
            }
        }

        public static void InvokeAutomationElement(AutomationElement automationElement)
        {
            var invokePattern = automationElement.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            invokePattern.Invoke();
        }

        private static AutomationElement GetLeftMonitorTaskBar()
        {
            AutomationElement desktop = AutomationElement.RootElement;
            AutomationElement tempElement = null;
            AutomationElementCollection collection;
            System.Windows.Rect boundingRect;
            object boundingRectNoDefault;

            collection = desktop.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ClassNameProperty, "Shell_SecondaryTrayWnd"));

            foreach (AutomationElement el in collection)
            {
                boundingRectNoDefault = el.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty, true);
                if (boundingRectNoDefault != AutomationElement.NotSupported)
                {
                    boundingRect = (System.Windows.Rect)boundingRectNoDefault;
                    if (boundingRect.Left < 0)
                    {
                        tempElement = el;
                    }
                }
            }

            return tempElement;
        }

        private static AutomationElementCollection GetTaskbarButtons(AutomationElement taskBar)
        {
            AutomationElementCollection buttons;

            taskBar = taskBar.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ClassNameProperty, "MSTaskListWClass"));
            buttons = taskBar.FindAll(TreeScope.Children, Condition.TrueCondition);

            return buttons;
        }
    }
}
