using System;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    /// <summary>
    /// ViewModel for job details
    /// </summary>
    public class JobDetailsViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;

        /// <summary>
        /// Constructor
        /// </summary>
        public JobDetailsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }
    }
}
