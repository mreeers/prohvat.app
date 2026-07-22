import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '../stores/auth'

class SignalRService {
  private connection: signalR.HubConnection | null = null
  private eventHandlers: { [eventName: string]: ((...args: any[]) => void)[] } = {}

  public startConnection() {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return
    }

    const authStore = useAuthStore()
    const token = authStore.token

    if (!token) {
      console.warn('Cannot start SignalR connection without a token.')
      return
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:8081/hubs/prohvat', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build()

    this.connection.start()
      .then(() => {
        console.log('SignalR connected.')
      })
      .catch((err: any) => {
        console.error('Error starting SignalR connection:', err)
      })

    // Register global events
    this.connection.on('ReceiveNotification', (notification: any) => {
      this.triggerEvent('ReceiveNotification', notification)
    })

    this.connection.on('ReceiveMessage', (message: any) => {
      this.triggerEvent('ReceiveMessage', message)
    })
  }

  public stopConnection() {
    if (this.connection) {
      this.connection.stop()
      this.connection = null
    }
  }

  // --- Chat specific methods ---
  public async joinConversation(conversationId: string) {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('JoinConversation', conversationId)
    }
  }

  public async leaveConversation(conversationId: string) {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveConversation', conversationId)
    }
  }

  public async sendChatMessage(conversationId: string, text: string) {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SendMessage', conversationId, text)
    }
  }

  public onReceiveMessage(handler: (message: any) => void) {
    this.on('ReceiveMessage', handler)
  }

  public offReceiveMessage(handler: (message: any) => void) {
    this.off('ReceiveMessage', handler)
  }

  // --- Custom Event Emitter ---
  public on(eventName: string, handler: (...args: any[]) => void) {
    if (!this.eventHandlers[eventName]) {
      this.eventHandlers[eventName] = []
    }
    this.eventHandlers[eventName].push(handler)
  }

  public off(eventName: string, handler: (...args: any[]) => void) {
    if (!this.eventHandlers[eventName]) return
    this.eventHandlers[eventName] = this.eventHandlers[eventName].filter(h => h !== handler)
  }

  private triggerEvent(eventName: string, ...args: any[]) {
    if (this.eventHandlers[eventName]) {
      this.eventHandlers[eventName].forEach(handler => handler(...args))
    }
  }
}

export const signalRService = new SignalRService()
