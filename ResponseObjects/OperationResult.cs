namespace Pack2SchoolFunction.Templates
{
    public class OperationResult
    {
        public string errorMessage;

        public bool requestSucceeded;

        public object data;

        public OperationResult()
        {
            this.requestSucceeded = true;
        }
  
        public void UpdateFailure (string errorMessage)
        {
            this.errorMessage = errorMessage;
            this.requestSucceeded = false;
        }

        public void UpdateData(object data)
        {
            this.data = data;
            this.requestSucceeded = true;

        }
    }
}
