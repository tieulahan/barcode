using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using Merit.BarCodeScanner.Data;
using Merit.BarCodeScanner.Helpers;
using Merit.BarCodeScanner.Logging;
using Merit.BarCodeScanner.Models;

namespace Merit.BarCodeScanner.Services
{
    public class BarCodeScannerService : IBarCodeScannerService
    {
        private ILogger _logService = new FileLogManager(typeof(BarCodeScannerService));


        public ResultRespose Import(ImportRequest request, AppSettingsSection _appSettings)
        {
            List<CsvValues> dailyScanValues = new List<CsvValues>();
            List<EmpWorking> empWorkings = new List<EmpWorking>();
            List<DeliveryBlock> deliveryBlocks = new List<DeliveryBlock>();

            List<PalletDetail> palletDetails = new List<PalletDetail>();
            EmpWorking empl = null;
            PalletDetail pallet = null;
            DeliveryBlock deliveryBlock = null;

            int row = 1;
            var blankRow = 0;
            var endBlock = 0;

            dailyScanValues = File.ReadAllLines(request.PathFile)
                              .Skip(0)
                              .Select(line => FileHelper.FromCsv(line, dailyScanValues.FindIndex(x => x.Equals(line))))
                              .ToList();

            foreach (var item in dailyScanValues.ToList())
            {
                var index = dailyScanValues.FindIndex(a => a.Id == item.Id);
                var prevIndex = index - 1;
                var nextIndex = index + 1;

                //Remove blank row
                //if (item.RowType.Equals(Contains.RowType.BLANK.ToString()))
                //{
                //    try
                //    {
                //        dailyScanValues.Remove(item);
                //        var nextItem = dailyScanValues[nextIndex];

                //        if (nextItem.RowType.Equals(Contains.RowType.BLANK.ToString()))
                //        {
                //            blankRow++;
                //        }

                //        if (blankRow <= 9)
                //        {
                //            _logService.LogError("Have multi blank row " + item.BarCode + ", Line: " + row);
                //            return new ResultRespose
                //            {
                //                Status = false,
                //                Message = "Have multi blank row " + item.BarCode + ", Line: " + row
                //            };
                //        }
                //    }
                //    catch (Exception) { }
                //}

                //Special character row
                if (item.RowType.Equals(Contains.RowType.ENDBLOCK.ToString()))
                {
                    try
                    {
                        var nextItem = dailyScanValues[nextIndex];

                        if (nextItem.RowType.Equals(Contains.RowType.ENDBLOCK.ToString()) || nextItem.RowType.Equals(Contains.RowType.EXCEPTION.ToString()))
                        {
                            endBlock++;

                            if (endBlock > 1)
                            {

                                _logService.LogError("Special character row " + nextItem.BarCode + ", Line: " + row);
                                return new ResultRespose
                                {
                                    Status = false,
                                    Message = "Special character row " + nextItem.BarCode + ", Line: " + row
                                };
                            }
                        }
                        //else if (nextItem.RowType.Equals(Contains.RowType.DESTINATION.ToString()))
                        //{
                        //    dailyScanValues.Remove(nextItem);
                        //    _logService.LogError("Redundant Destination barcode " + nextItem.BarCode + ", Line: " + row);
                        //    return new ResultRespose
                        //    {
                        //        Status = false,
                        //        Message = "Redundant Destination barcode " + nextItem.BarCode + ", Line: " + row
                        //    };
                        //}
                    }
                    catch (Exception) { }
                }

                if (item.IsBarCode && row > 0 &&
                    item.RowType.Equals(Contains.RowType.DESTINATION.ToString()))
                {

                    try
                    {
                        var nextItem = dailyScanValues[nextIndex];
                        var prevItem = dailyScanValues[prevIndex];
                        //Lack Pallet barcode in working block (Destination -> Employee -> Destination)
                        if (prevItem.RowType.Equals(Contains.RowType.EMPLOYEE.ToString()))
                        {
                            dailyScanValues.Remove(item);
                            _logService.LogWarn("Redundant Employee barcode " + item.BarCode + ", Line: " + row);
                        }

                        //Destination barcodes are displayed at the beginning of Scanner dataset                    
                        if (prevItem.RowType.Equals(Contains.RowType.ENDBLOCK.ToString()))
                        {
                            dailyScanValues.Remove(item);
                            _logService.LogWarn("Redundant Destination barcode " + item.BarCode + ", Line: " + row);
                        }

                        //Destination barcodes are displayed continuously in multi rows                
                        deliveryBlock = new DeliveryBlock
                        {
                            BlockId = palletDetails.Last().BlockId,
                            DestinationId = item.BarCode,
                            BlockEndTime = empWorkings.Last().WorkTime,
                            BlockStartTime = FileHelper.CvStringToDate(item.DateTime)
                        };

                        if (nextItem.RowType.Equals(Contains.RowType.DESTINATION.ToString())
                            && nextItem.BarCode.Equals(item.BarCode))
                        {
                            _logService.LogWarn("Redundant Destination barcode " + item.BarCode + ", Line: " + row);
                        }

                        if (nextItem.RowType.Equals(Contains.RowType.EMPLOYEE.ToString()))
                        {
                            deliveryBlocks.Add(deliveryBlock);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                //Format is not correct
                if (item.RowType.Equals(Contains.RowType.UNDEFINED.ToString()))
                {
                    dailyScanValues.Remove(item);
                    _logService.LogWarn("EEID format is not correct " + item.BarCode + ", Line: " + row);
                }

                //Lacking date info
                if (item.IsBarCode && string.IsNullOrEmpty(item.Date))
                {
                    _logService.LogError("Lacking date " + item.BarCode + ", Line: " + row);
                    return new ResultRespose
                    {
                        Status = false,
                        Message = "Lacking date " + item.BarCode + ", Line: " + row
                    };
                }

                //Lacking time info
                if (item.IsBarCode && string.IsNullOrEmpty(item.Time))
                {
                    _logService.LogError("Lacking time " + item.BarCode + ", Line: " + row);
                    return new ResultRespose
                    {
                        Status = false,
                        Message = "Lacking time " + item.BarCode + ", Line: " + row
                    };
                }

                //Datetime Format is not correct
                if (item.IsBarCode
                    && !string.IsNullOrEmpty(item.Date)
                    && !string.IsNullOrEmpty(item.Time)
                     && FileHelper.CvStringToDate(item.DateTime) == null)
                {
                    _logService.LogError("Datetime Format is not correct " + item.BarCode + ", Line: " + row);
                    return new ResultRespose
                    {
                        Status = false,
                        Message = "Lacking time " + item.BarCode + ", Line: " + row
                    };
                }

                //Lacking Barcode
                if (item.IsBarCode
                    && string.IsNullOrEmpty(item.BarCode))
                {
                    _logService.LogError("Lacking Barcode " + item.BarCode + ", Line: " + row);
                    return new ResultRespose
                    {
                        Status = false,
                        Message = "Lacking Barcode " + item.BarCode + ", Line: " + row
                    };
                }

                //Must have Emplyee ID at the first row of each scanner dataset
                if (row == 1 && (item.RowType.Equals(Contains.RowType.PALLET.ToString())
                    || item.RowType.Equals(Contains.RowType.DESTINATION.ToString())))
                {
                    _logService.LogError("Lacking Employee barcode " + item.BarCode + ", Line: " + row);
                    return new ResultRespose
                    {
                        Status = false,
                        Message = "Lacking Employee barcode " + item.BarCode + ", Line: " + row
                    };
                }

                //Multi Employee barcodes are displayed continuously in multi rows
                if (item.IsBarCode
                    && !string.IsNullOrEmpty(item.BarCode)
                    && item.RowType.Equals(Contains.RowType.EMPLOYEE.ToString()))
                {
                    //Lack Pallet barcode in working block (Pallet barcode-> Employee -> Destination)
                    try
                    {
                        var nextItem = dailyScanValues[nextIndex];
                        var prevItem = dailyScanValues[prevIndex];
                        if (prevItem.RowType.Equals(Contains.RowType.PALLET.ToString())
                            && nextItem.RowType.Equals(Contains.RowType.DESTINATION.ToString()))
                        {
                            dailyScanValues.Remove(item);
                            _logService.LogWarn("Redundant Employee barcode " + item.BarCode + ", Line: " + row);
                        }

                        //if (nextItem.RowType.Equals(Contains.RowType.DESTINATION.ToString()))
                        //{
                        //    dailyScanValues.Remove(nextItem);
                        //    _logService.LogWarn("Redundant Employee barcode " + nextItem.BarCode + ", Line: " + row);
                        //}
                    }
                    catch (Exception) { }

                    try
                    {
                        var nextItem = dailyScanValues[nextIndex];

                        //Employee barcode is displayed at the end of Scanner dataset
                        if (nextItem.RowType.Equals(Contains.RowType.ENDBLOCK.ToString()))
                        {
                            dailyScanValues.Remove(item);
                            _logService.LogWarn("Redundant Employee barcode " + item.BarCode + ", Line: " + row);
                        }
                    }
                    catch (Exception) { }

                    if (empWorkings.Any())
                    {
                        if (dailyScanValues[prevIndex].IsBarCode
                            && dailyScanValues[prevIndex].RowType.Equals(Contains.RowType.EMPLOYEE.ToString()))
                        {
                            var prevItem = dailyScanValues[prevIndex];
                            empWorkings.Remove(empWorkings.Last());
                            int prevRow = row - 1;

                            _logService.LogWarn("Multi Employee Barcode " + prevItem.BarCode + ", Line: " + prevRow);
                        }
                    }

                    empl = new EmpWorking
                    {
                        Id = Guid.NewGuid(),
                        Day = item.Date,
                        EmployeeId = item.BarCode,
                        WorkTime = FileHelper.CvStringToDate(item.DateTime)
                    };
                    empWorkings.Add(empl);
                }

                ////Insert EmployeeID to List                
                //if (prevItem.RowType.Equals(Contains.RowType.DESTINATION.ToString()))
                //{
                //    empWorkings.Add(empl);
                //}

                //Lack Employee barcode after 1 Destination barcode (have Pallet barcode after Destination barcode)
                if (item.IsBarCode && row > 0
                    && !string.IsNullOrEmpty(item.BarCode)
                    && item.RowType.Equals(Contains.RowType.PALLET.ToString()))
                {
                    index = dailyScanValues.FindIndex(a => a.Id == item.Id) - 1;
                    var prevItem = dailyScanValues[prevIndex];

                    prevItem = dailyScanValues[index];

                    if (prevItem.RowType.Equals(Contains.RowType.DESTINATION.ToString()))
                    {
                        var lastEmp = empWorkings.Last();
                        empl = new EmpWorking
                        {
                            Id = Guid.NewGuid(),
                            Day = lastEmp.Day,
                            EmployeeId = lastEmp.EmployeeId,
                            WorkTime = FileHelper.CvStringToDate(item.DateTime).Value.AddSeconds(-1)
                        };
                        empWorkings.Add(empl);
                        _logService.LogWarn("Lack Employee barcode befor Pallet " + item.BarCode + ", Line: " + row);
                    }

                    //Remove duplicate pallet
                    if (palletDetails.Any(p=>p.PalletId  == item.BarCode))
                    {
                        var listDuplicate =
                            palletDetails.FirstOrDefault(x => x.PalletId == item.BarCode && x.Day == item.Date);

                        if (listDuplicate != null)
                        {
                            //Create new Pallet
                            pallet = new PalletDetail
                            {
                                BlockId = empWorkings.Last().Id,
                                Day = empWorkings.Last().Day,
                                EmployeeId = empWorkings.Last().EmployeeId,
                                PalletId = item.BarCode,
                                PalletScanTime = FileHelper.CvStringToDate(item.DateTime)
                            };

                            _logService.LogWarn("Pallet Barcode is duplicated " + item.BarCode + ", Line: " + row);
                        }
                    }
                    else
                    {
                        //Create new Pallet
                        pallet = new PalletDetail
                        {
                            BlockId = empWorkings.Last().Id,
                            Day = empWorkings.Last().Day,
                            EmployeeId = empWorkings.Last().EmployeeId,
                            PalletId = item.BarCode,
                            PalletScanTime = FileHelper.CvStringToDate(item.DateTime)
                        };
                    }

                    palletDetails.Add(pallet);
                }
                row++;
            }

            //Insert to DB
            using (var dbContext = new barCodeDbContext())
            {
                //foreach (var emp in empWorkings)
                //{
                //    var getData = dbContext.EmpWorkings.FirstOrDefault(x => x.EmployeeId == emp.EmployeeId
                //    && x.Day == emp.Day);

                //    if (getData == null)
                //    {
                //        dbContext.EmpWorkings.AddOrUpdate(emp);
                //    }
                //}

                //foreach (var item in palletDetails)
                //{
                //    dbContext.PalletDetails.AddOrUpdate(item);
                //}

                //foreach (var item in deliveryBlocks)
                //{
                //    dbContext.DeliveryBlocks.AddOrUpdate(item);
                //}

                dbContext.SaveChanges();
            }

            return new ResultRespose
            {
                Status = true,
            };
        }
    }
}
