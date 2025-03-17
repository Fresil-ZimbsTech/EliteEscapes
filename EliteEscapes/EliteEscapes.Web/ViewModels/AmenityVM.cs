using EliteEscapes.Domain.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EliteEscapes.Web.ViewModels
{
    public class AmenityVM
    {
        //dropdown list for villa number`
        public Amenity? Amenity { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem>? VillaList { get; set; }  
    }
}
