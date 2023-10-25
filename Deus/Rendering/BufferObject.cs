﻿using Silk.NET.OpenGL;

namespace DeusEngine;

//Our buffer object abstraction.
public class BufferObject<TDataType> : IDisposable
    where TDataType : unmanaged
{
    //Our handle, buffer type and the GL instance this class will use, these are private because they have no reason to be public.
    //Most of the time you would want to abstract items to make things like this invisible.
    private uint _handle;
    private BufferTargetARB _bufferType;
    private GL _gl;
    public int Length = 0;

    public unsafe BufferObject( Span<TDataType> data, BufferTargetARB bufferType)
    {
        //Setting the gl instance and storing our buffer type.
        _gl = RenderingEngine.Gl;
        _bufferType = bufferType;

        //Getting the handle, and then uploading the data to said handle.
        _handle = _gl.GenBuffer();
        Bind();
        fixed (void* d = data)
        {
            _gl.BufferData(bufferType, (nuint) (data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
        }
        
        //Storing the length of the data.
        Length = data.Length;
        
    }

    public void Bind()
    {
        //Binding the buffer object, with the correct buffer type.
        _gl.BindBuffer(_bufferType, _handle);
    }

    public void Dispose()
    {
        //Remember to delete our buffer.
        _gl.DeleteBuffer(_handle);
    }
}