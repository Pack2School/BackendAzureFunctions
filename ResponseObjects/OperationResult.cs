using System;
using System.Collections.Generic;
using System.Text;

namespace Pack2SchoolFunction.Templates
{
    /// <summary>
    /// 
    /// </summary>
    public class OperationResult
    {
        /// <summary>
        /// cause of failure 
        /// </summary>
        public string errorMessage;

        /// <summary>
        /// flag indicating whether the operation was successful
        /// </summary>
        public bool requestSucceeded;

        /// <summary>
        /// operatio output
        /// </summary>
        public object data;

        /// <summary>
        /// constructor
        /// </summary>
        public OperationResult()
        {
            this.requestSucceeded = true;
        }
  
        /// <summary>
        /// Updates the response in case of failed operation
        /// </summary>
        /// <param name="errorMessage">description of the cause of the failure</param>
        public void UpdateFailure (string errorMessage)
        {
            this.errorMessage = errorMessage;
            this.requestSucceeded = false;
        }

        /// <summary>
        /// Updates the response in case of successful operaion 
        /// </summary>
        /// <param name="data">operation output</param>
        public void UpdateData(object data)
        {
            this.data = data;
            this.requestSucceeded = true;

        }
    }
}
