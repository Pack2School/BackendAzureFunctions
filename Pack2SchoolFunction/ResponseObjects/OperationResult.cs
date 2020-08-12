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
        public string ErrorMessage;

        /// <summary>
        /// flag indicating whether the operation was successful
        /// </summary>
        public bool RequestSucceeded;

        /// <summary>
        /// operatio output
        /// </summary>
        public object Data;

        /// <summary>
        /// constructor
        /// </summary>
        public OperationResult()
        {
            this.RequestSucceeded = true;
        }
  
        /// <summary>
        /// Updates the response in case of failed operation
        /// </summary>
        /// <param name="errorMessage">description of the cause of the failure</param>
        public void UpdateFailure (string errorMessage)
        {
            this.ErrorMessage = errorMessage;
            this.RequestSucceeded = false;
        }

        /// <summary>
        /// Updates the response in case of successful operaion 
        /// </summary>
        /// <param name="data">operation output</param>
        public void UpdateData(object data)
        {
            this.Data = data;
            this.RequestSucceeded = true;
        }

    }
}
