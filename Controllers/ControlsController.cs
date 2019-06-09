﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using openstig_api_controls.Models;
using openstig_api_controls.Database;
using System.IO;
using System.Text;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Xml.Serialization;
using System.Xml;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace openstig_api_controls.Controllers
{
    [Route("/")]
    public class ControlsController : Controller
    {
        private readonly ILogger<ControlsController> _logger;
        private ControlsDBContext _context;

        public ControlsController(ILogger<ControlsController> logger, ControlsDBContext context)
        {
            _logger = logger;
            _context = context; // pass in the database in memory
        }

        // GET the full listing of NIST 800-53 controls
        [HttpGet]
        public async Task<IActionResult> GetAllControls(string filter = "")
        {
            try {
                var result = await _context.ControlSets.ToListAsync();
                if (result != null)
                    if (string.IsNullOrEmpty(filter))
                        return Ok(result);
                    else {
                        if (filter.Trim().ToLower() == "low")
                            return Ok(result.Where(x => x.lowimpact).ToList());
                        else if (filter.Trim().ToLower() == "moderate")
                            return Ok(result.Where(x => x.moderateimpact).ToList());
                        else if (filter.Trim().ToLower() == "high")
                            return Ok(result.Where(x => x.highimpact).ToList());
                        else 
                            return Ok(result);
                    }
                else
                    return NotFound(); // nothing loaded yet
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error listing all control sets. Please check the in memory database and XML file load.");
                return BadRequest();
            }
        }
                
        // GET the text of a control passed in from the compliance page when you click on an individual 
        // item on a single-checklist page that is filtered based on compliance
        [HttpGet("{term}")]
        public async Task<IActionResult> GetControl(string term)
        {
            if (!string.IsNullOrEmpty(term)) {
                try {
                    string searchTerm = term.Replace(" ", ""); // get rid of things we do not need
                    var result = await _context.ControlSets.Where(x => x.subControlNumber == searchTerm || x.number == searchTerm).ToListAsync();
                    if (result != null && result.Count > 0)
                        return Ok(result);
                    else { // try to get the main family description and return that
                        int index = GetFirstIndex(term);
                        if (index < 0)
                            return NotFound(); // nothing loaded yet
                        else { // see if there is a family title we can pass back
                            searchTerm = term.Substring(0, index).Trim();
                            result = await _context.ControlSets.Where(x => x.subControlNumber == searchTerm || x.number == searchTerm).ToListAsync();
                            if (result != null && result.Count > 0)
                                return Ok(result.FirstOrDefault());
                            else
                                return NotFound();
                        }
                    }
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error listing all control sets. Please check the in memory database and XML file load.");
                    return BadRequest();
                }
            }
            else
                return NotFound();
        }

        private int GetFirstIndex(string term) {
            int space = term.IndexOf(" ");
            int period = term.IndexOf(".");
            if (space < 0 && period < 0)
                return -1;
            else if (space > 0 && period > 0 && space < period ) // see which we hit first
                return space;
            else if (space > 0 && period > 0 && space > period )
                return period;
            else if (space > 0) 
                return space;
            else 
                return period;
        }

    }
}
