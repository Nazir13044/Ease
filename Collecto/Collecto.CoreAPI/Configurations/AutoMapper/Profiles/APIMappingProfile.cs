using AutoMapper;
using Collecto.CoreAPI.Models.Objects.Systems;
using Collecto.CoreAPI.Models.Responses.Setups;

namespace Collecto.CoreAPI.Configurations.AutoMapper.Profiles
{
    /// <summary>
    /// 
    /// </summary>
    public class APIMappingProfile : Profile
    {
        /// <summary>
        /// 
        /// </summary>
        public APIMappingProfile()
        {
            CreateMap<User, LoginResponse>().ReverseMap();
        }
    }
}
