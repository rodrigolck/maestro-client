﻿using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class MaestroClient {
  public static bool AutoPingEnable = true;

  public enum Status {
    Ready,
    Occupied,
    Terminating,
    Terminated,
  }

  public enum Event {
    Join,
    Left
  }

  [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void DebugLog(string log);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void OnTerminating();

  private static readonly DebugLog debugLog = DebugWrapper;
  private static readonly IntPtr functionPointer = Marshal.GetFunctionPointerForDelegate(debugLog);
  private static IntPtr maestroClient = IntPtr.Zero;
  private static float PingInterval = 30;
  private static bool initialized = false;

  private static void DebugWrapper(string log) {
#if DEBUG
    Debug.Log("Maestro:" + log);
#endif
  }

  public static string Address {
    get {
      if (!IsInitialized) return null;

      IntPtr ptr = GetAddressInternal(maestroClient);
      return Marshal.PtrToStringAuto(ptr);
    }
  }

  private static bool IsInitialized {
    get {
      if (!initialized) {
        Debug.LogError("Maestro: calling methods before initialization!");
      }
      return initialized;
    }
  }

  public static void Initialize(string apiUrl, string scheduler = null, string id = null, bool autoPing = true, float pingInterval = 30.0f, OnTerminating onTerminatingFunction = null) {
    if (!initialized) {
      LinkDebugInternal (functionPointer);
      if (onTerminatingFunction != null) {
        OnTerminating terminatingFunction = onTerminatingFunction;
        IntPtr terminateFunctionPointer = Marshal.GetFunctionPointerForDelegate (terminatingFunction);
        LinkOnTerminatingInternal (terminateFunctionPointer);
      }
      maestroClient = ClientInternal (apiUrl, 0);

      var initilized = false;

      if (string.IsNullOrEmpty (scheduler) || string.IsNullOrEmpty (id)) {
        initilized = InitializeInternal (maestroClient);
      } else {
        initilized = InitializeInternal (maestroClient, scheduler, id);
      }

      if (initilized) {
        Debug.Log ("Maestro client initialized successfully");
      } else {
        Debug.LogError ("Maestro client failed to initialize 😿 : " + apiUrl);
      }

      AutoPingEnable = autoPing;
      PingInterval = pingInterval;

      initialized = true;
    }
  }

  public static void Ping() {
    if (!IsInitialized) return;
    PingInternal(maestroClient);
  }

  public static string PlayerEvent(Event playerEvent) {
    if (!IsInitialized) return;
    switch (playerEvent) {
      case Event.Join:
        IntPtr ptr = PlayerJoinInternal (maestroClient);
        return Marshal.PtrToStringAuto(ptr);
        break;
      case Event.Left:
        IntPtr ptr = PlayerLeftInternal (maestroClient);
        return Marshal.PtrToStringAuto(ptr);
        break;
    }
  }

  public static string PlayerEvent(Event playerEvent, string metadata) {
    if (!IsInitialized) return;
    switch (playerEvent) {
      case Event.Join:
        IntPtr ptr = PlayerJoinInternal (maestroClient, metadata);
        return Marshal.PtrToStringAuto(ptr);
        break;
      case Event.Left:
        IntPtr ptr = PlayerLeftInternal (maestroClient, metadata);
        return Marshal.PtrToStringAuto(ptr);
        break;
    }
  }

  public static string RoomEvent(string event) {
    if (!IsInitialized) return;
    IntPtr ptr = RoomEventInternal (maestroClient, event);
    return Marshal.PtrToStringAuto(ptr);
  }

  public static string RoomEvent(string event, string metadata) {
    if (!IsInitialized) return;
    IntPtr ptr = RoomEventInternal (maestroClient, event, metadata);
    return Marshal.PtrToStringAuto(ptr);
  }

  public static void UpdateStatus(Status status) {
    if (!IsInitialized) return;

    switch (status) {
      case Status.Ready:
        RoomReadyInternal(maestroClient);
        break;
      case Status.Occupied:
        RoomOccupiedInternal(maestroClient);
        break;
      case Status.Terminating:
        RoomTerminatingInternal(maestroClient);
        break;
      case Status.Terminated:
        RoomTerminatedInternal(maestroClient);
        break;
    }
  }

  public static void UpdateStatus(Status status, string metadata) {
    if (!IsInitialized) return;

    switch (status) {
      case Status.Ready:
        RoomReadyInternal(maestroClient,metadata);
        break;
      case Status.Occupied:
        RoomOccupiedInternal(maestroClient,metadata);
        break;
      case Status.Terminating:
        RoomTerminatingInternal(maestroClient,metadata);
        break;
      case Status.Terminated:
        RoomTerminatedInternal(maestroClient,metadata);
        break;
    }
  }

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_get_address")]
    private static extern IntPtr GetAddressInternal(IntPtr obj);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_get_ping_interval")]
    private static extern int GetPingIntervalInternal(IntPtr obj);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_initialize")]
    private static extern bool InitializeInternal(IntPtr obj);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_initialize_with_scheduler_and_id")]
    private static extern bool InitializeInternal(IntPtr obj, string scheduler, string id);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "set_debug")]
    private static extern void LinkDebugInternal([MarshalAs(UnmanagedType.FunctionPtr)] IntPtr debugCal);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "set_on_terminating")]
    private static extern void LinkOnTerminatingInternal([MarshalAs(UnmanagedType.FunctionPtr)] IntPtr onTerminatingCal);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_create_client_with_ping_interval")]
    private static extern IntPtr ClientInternal(string maestroApiUrl, int pingInterval);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_ping")]
    private static extern bool PingInternal(IntPtr obj);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_room_occupied")]
    private static extern bool RoomOccupiedInternal(IntPtr obj);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_room_ready")]
    private static extern bool RoomReadyInternal(IntPtr obj);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_room_terminated")]
    private static extern bool RoomTerminatedInternal(IntPtr obj);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_room_terminating")]
    private static extern bool RoomTerminatingInternal(IntPtr obj);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_room_occupied_with_metadata")]
    private static extern bool RoomOccupiedInternal(IntPtr obj, string metadata);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_room_ready_with_metadata")]
    private static extern bool RoomReadyInternal(IntPtr obj, string metadata);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_room_terminated_with_metadata")]
    private static extern bool RoomTerminatedInternal(IntPtr obj, string metadata);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_room_terminating_with_metadata")]
    private static extern bool RoomTerminatingInternal(IntPtr obj, string metadata);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_set_ping_interval")]
    private static extern void SetPingIntervalInternal(IntPtr obj, int pingInterval);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_player_join_with_metadata")]
    private static extern IntPtr PlayerJoinInternal(IntPtr obj, string metadata);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_player_left_with_metadata")]
    private static extern IntPtr PlayerLeftInternal(IntPtr obj, string metadata);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_player_join")]
    private static extern IntPtr PlayerJoinInternal(IntPtr obj);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_player_left")]
    private static extern IntPtr PlayerLeftInternal(IntPtr obj);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_room_event_with_metadata")]
    private static extern IntPtr RoomEventInternal(IntPtr obj, string event, string metadata);

  [DllImport("libmaestro", CallingConvention = CallingConvention.Cdecl, EntryPoint = "internal_room_event")]
    private static extern IntPtr RoomEventInternal(IntPtr obj, string event);
}
