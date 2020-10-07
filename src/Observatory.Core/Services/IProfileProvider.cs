using Observatory.Core.Models;
using Observatory.Core.ViewModels;
using System.Threading.Tasks;

namespace Observatory.Core.Services
{
    /// <summary>
    /// Defines a service that knows how to instantiate a concrete profile register and its view model.
    /// </summary>
    public interface IProfileProvider
    {
        string DisplayName { get; }
        string IconGeometry { get; }

        /// <summary>
        /// Creates an instance of profile view model from its registration.
        /// </summary>
        /// <param name="register">The regiser to be created from.</param>
        /// <returns>An instance of the profile.</returns>
        Task<ProfileViewModelBase> CreateViewModelAsync(ProfileRegister register);

        /// <summary>
        /// Authenticates the user and creates a corresponding profile register.
        /// </summary>
        /// <param name="profileDataDirectory">The directory to the file storing the profile's data.</param>
        /// <returns></returns>
        Task<ProfileRegister> CreateRegisterAsync(string profileDataDirectory);
    }
}
