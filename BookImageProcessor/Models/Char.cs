﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookImageProcessor.Models
{
    public class Char
    {

        public string BoundingBox { get; set; }
        public Microsoft.ProjectOxford.Vision.Contract.Rectangle Rectangle { get; }
        public string TextSingleChar { get; set; }

    }
}
