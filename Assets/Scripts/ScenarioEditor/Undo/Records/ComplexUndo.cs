/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using System.Collections.Generic;
    using Managers;

    /// <summary>
    /// Complex undo record that contains multiple undo records that are handles at once
    /// </summary>
    public class ComplexUndo : UndoRecord
    {
        /// <summary>
        /// Subrecords that will be handled by this record
        /// </summary>
        private readonly List<UndoRecord> records;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="records">Subrecords that will be handled by this record</param>
        public ComplexUndo(List<UndoRecord> records)
        {
            this.records = records;
        } 
        
        /// <inheritdoc/>
        public override void Undo()
        {
            for (int i = 0; i < records.Count; i++) records[i].Undo();
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            for (int i = 0; i < records.Count; i++) records[i].Dispose();
        }
    }
}