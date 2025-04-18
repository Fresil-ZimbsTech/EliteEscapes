﻿using EliteEscapes.Domain.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EliteEscapes.Web.ViewModels
{
    public class VillaNumberVM
    {
        //dropdown list for villa number`
        public VillaNumber? VillaNumber { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem>? VillaList { get; set; }  
    }
}
