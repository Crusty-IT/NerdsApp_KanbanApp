import { useState } from 'react';
import api from '../services/api';

const COLORS = [
    '#00d4ff', '#3b82f6', '#6366f1', '#8b5cf6',
    '#10b981', '#14b8a6', '#f59e0b', '#ef4444',
    '#ec4899', '#f97316', '#84cc16', '#06b6d4'
];

export function useColumns(boardId, board, setBoard) {
    const [columnName, setColumnName] = useState('');
    const [columnColor, setColumnColor] = useState(COLORS[0]);
    const [showColumnForm, setShowColumnForm] = useState(false);
    const [isEditingColumn, setIsEditingColumn] = useState(false);
    const [editingColumn, setEditingColumn] = useState(null);
    const [showConfirmDelete, setShowConfirmDelete] = useState(false);
    const [pendingDeleteColumnId, setPendingDeleteColumnId] = useState(null);
    const [error, setError] = useState(null);

    const showError = (msg) => {
        setError(msg);
        setTimeout(() => setError(null), 3000);
    };

    const handleCreateColumn = async (e) => {
        e.preventDefault();
        if (!columnName.trim()) return;
        try {
            const position = board.columns.length;
            const response = await api.post(`/api/boards/${boardId}/columns`, {
                name: columnName.trim(), position, color: columnColor
            });
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                updated.columns.push({ ...response.data, cards: [] });
                return updated;
            });
            closeColumnForm();
        } catch {
            showError('Failed to create column');
        }
    };

    const handleEditColumn = (column) => {
        setEditingColumn(column);
        setColumnName(column.name);
        setColumnColor(column.color);
        setIsEditingColumn(true);
        setShowColumnForm(true);
    };

    const handleUpdateColumn = async (e) => {
        e.preventDefault();
        if (!columnName.trim()) return;
        try {
            const response = await api.put(`/api/boards/${boardId}/columns/${editingColumn.id}`, {
                name: columnName.trim(), color: columnColor
            });
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                const col = updated.columns.find(c => c.id === editingColumn.id);
                col.name = response.data.name;
                col.color = response.data.color;
                return updated;
            });
            closeColumnForm();
        } catch {
            showError('Failed to update column');
        }
    };

    const handleDeleteColumn = (columnId) => {
        setPendingDeleteColumnId(columnId);
        setShowConfirmDelete(true);
    };

    const confirmDeleteColumn = async () => {
        try {
            await api.delete(`/api/boards/${boardId}/columns/${pendingDeleteColumnId}`);
            setBoard(prev => {
                const updated = JSON.parse(JSON.stringify(prev));
                updated.columns = updated.columns.filter(c => c.id !== pendingDeleteColumnId);
                return updated;
            });
        } catch {
            showError('Failed to delete column');
        } finally {
            setShowConfirmDelete(false);
            setPendingDeleteColumnId(null);
        }
    };

    const closeColumnForm = () => {
        setShowColumnForm(false);
        setIsEditingColumn(false);
        setEditingColumn(null);
        setColumnName('');
        setColumnColor(COLORS[0]);
        setError(null);
    };

    return {
        columnName, setColumnName,
        columnColor, setColumnColor,
        showColumnForm, setShowColumnForm,
        isEditingColumn, editingColumn,
        showConfirmDelete, setShowConfirmDelete,
        pendingDeleteColumnId,
        error,
        COLORS,
        handleCreateColumn,
        handleEditColumn,
        handleUpdateColumn,
        handleDeleteColumn,
        confirmDeleteColumn,
        closeColumnForm
    };
}