﻿using System.Collections.Generic;
using System.Linq;
using BrightWire.Models;
using BrightWire.ExecutionGraph.Helper;

namespace BrightWire.ExecutionGraph.DataTableAdaptor
{
    /// <summary>
    /// Adapts data tables that classify tensors (volumes)
    /// </summary>
    class TensorBasedDataTableAdaptor : RowBasedDataTableAdaptorBase, IVolumeDataSource
    {
        readonly int _inputSize, _outputSize, _rows, _columns, _depth;
        readonly List<(IContext Context, int Rows, int Columns, int Depth)> _processedContext = new List<(IContext, int, int, int)>();

        public TensorBasedDataTableAdaptor(ILinearAlgebraProvider lap, IDataTable dataTable)
            : base(lap, dataTable)
        {
            var firstRow = dataTable.GetRow(0);
            var input = (FloatTensor)firstRow.Data[_dataColumnIndex[0]];
            var output = (FloatVector)firstRow.Data[_dataTargetIndex];
            _outputSize = output.Size;
            _inputSize = input.Size;
            _rows = input.RowCount;
            _columns = input.ColumnCount;
            _depth = input.Depth;
        }

        private TensorBasedDataTableAdaptor(ILinearAlgebraProvider lap, IDataTable dataTable, int inputSize, int outputSize, int rows, int columns, int depth) 
            :base(lap, dataTable)
        {
            _inputSize = inputSize;
            _outputSize = outputSize;
            _rows = rows;
            _columns = columns;
            _depth = depth;
        }

        public override IDataSource CloneWith(IDataTable dataTable)
        {
            return new TensorBasedDataTableAdaptor(_lap, dataTable, _inputSize, _outputSize, _rows, _columns, _depth);
        }

        public override bool IsSequential => false;
        public override int InputSize => _inputSize;
        public override int OutputSize => _outputSize;
        public int Width => _columns;
        public int Height => _rows;
        public int Depth => _depth;

        public override IMiniBatch Get(IExecutionContext executionContext, IReadOnlyList<int> rows)
        {
            var data = _GetRows(rows)
                .Select(r => (_dataColumnIndex.Select(i => ((FloatTensor)r.Data[i]).GetAsRaw()).ToList(), ((FloatVector)r.Data[_dataTargetIndex]).Data))
                .ToList()
            ;
            var inputList = new List<IGraphData>();
            for (var i = 0; i < _dataColumnIndex.Length; i++) {
	            var i1 = i;
	            var input = _lap.CreateMatrix(InputSize, data.Count, (x, y) => data[y].Item1[i1][x]);
                var tensor = new Tensor4DGraphData(input, _rows, _columns, _depth);
                inputList.Add(tensor);
            }
            var output = OutputSize > 0 ? _lap.CreateMatrix(data.Count, OutputSize, (x, y) => data[x].Item2[y]) : null;
            
            return new MiniBatch(rows, this, inputList, new MatrixGraphData(output));
        }
    }
}
