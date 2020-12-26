using Observatory.Core.Models;
using Observatory.Core.ViewModels;
using System.IO;
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
        /// <returns>An instance of <see cref="ProfileViewModelBase"/>.</returns>
        Task<ProfileViewModelBase> CreateViewModelAsync(ProfileRegister register);

        /// <summary>
        /// Authenticates the user and creates a corresponding profile register.
        /// </summary>
        /// <param name="profileDataDirectory">The directory to the file storing the profile's data.</param>
        /// <returns>An instance of <see cref="ProfileRegister"/>.</returns>
        Task<ProfileRegister> CreateRegisterAsync(string profileDataDirectory);

        /// <summary>
        /// Loads the icon stream for use in the UI to represent the provider.
        /// </summary>
        /// <returns>An instance of <see cref="Stream"/> to read the icon from.</returns>
        Stream LoadIconStream();
    }
}
