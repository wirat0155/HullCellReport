using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HullCellReport.Models
{
    public class CreateReportFM
    {
        // UUID
        public string txt_uuid { get; set; }

        // Section 1: Hull Cell Report
        public string txt_line { get; set; }
        public string txt_analysis_by { get; set; }
        public string txt_sampling_date { get; set; }
        public string txt_time { get; set; }
        public string txt_remark { get; set; }

        // Composition Analysis
        public decimal? txt_zinc_metal { get; set; }
        public decimal? txt_caustic_soda { get; set; }
        public decimal? txt_sodium_carbonate { get; set; }
        public decimal? txt_nickel { get; set; }
        public decimal? txt_us_208t { get; set; }
        public string txt_ratio { get; set; }

        // Section 2: Parameter Table
        public string txt_temp_start { get; set; }
        public string txt_temp_finish { get; set; }
        public string txt_voltage_start { get; set; }
        public string txt_voltage_finish { get; set; }

        // Section 3: X-ray Program
        public string txt_result_1cm { get; set; }
        public string txt_zn_1cm { get; set; }
        public string txt_ni_1cm { get; set; }

        public string txt_result_3cm { get; set; }
        public string txt_zn_3cm { get; set; }
        public string txt_ni_3cm { get; set; }

        public string txt_result_5cm { get; set; }
        public string txt_zn_5cm { get; set; }
        public string txt_ni_5cm { get; set; }

        public string txt_result_7cm { get; set; }
        public string txt_zn_7cm { get; set; }
        public string txt_ni_7cm { get; set; }

        public string txt_result_9cm { get; set; }
        public string txt_zn_9cm { get; set; }
        public string txt_ni_9cm { get; set; }

        public string txt_result_19cm { get; set; }
        public string txt_zn_19cm { get; set; }
        public string txt_ni_19cm { get; set; }

        public string txt_max_result { get; set; }
        public string txt_max_zn { get; set; }
        public string txt_max_ni { get; set; }

        public string txt_min_result { get; set; }
        public string txt_min_zn { get; set; }
        public string txt_min_ni { get; set; }

        // Section 5: Adjustment Table
        // Zn
        public string txt_batch_zn { get; set; }
        public string txt_adjust_zn { get; set; }
        public string txt_auto_feed_zn { get; set; }
        public string txt_remark_zn { get; set; }

        // NaOH
        public string txt_batch_naoh { get; set; }
        public string txt_adjust_naoh { get; set; }
        public string txt_auto_feed_naoh { get; set; }
        public string txt_remark_naoh { get; set; }

        // 208N
        public string txt_batch_208n { get; set; }
        public string txt_adjust_208n { get; set; }
        public string txt_auto_feed_208n { get; set; }
        public string txt_remark_208n { get; set; }

        // 208T
        public string txt_batch_208t { get; set; }
        public string txt_adjust_208t { get; set; }
        public string txt_auto_feed_208t { get; set; }
        public string txt_remark_208t { get; set; }

        // 208A
        public string txt_batch_208a { get; set; }
        public string txt_adjust_208a { get; set; }
        public string txt_auto_feed_208a { get; set; }
        public string txt_remark_208a { get; set; }

        // 208B
        public string txt_batch_208b { get; set; }
        public string txt_adjust_208b { get; set; }
        public string txt_auto_feed_208b { get; set; }
        public string txt_remark_208b { get; set; }

        // Section 6: Upload
        [JsonIgnore] // Don't serialize IFormFile to JSON
        public List<IFormFile> txt_file_upload { get; set; }
        public List<string> txt_uploaded_images { get; set; } // Store image filenames

        // Tracking Section
        public string txt_status {get;set;}
        public string txt_creuser {get; set;}
        public string txt_updateuser {get; set;}
        public DateTime txt_credate {get; set;}
        public DateTime txt_updatedate { get; set; }
    }
}
