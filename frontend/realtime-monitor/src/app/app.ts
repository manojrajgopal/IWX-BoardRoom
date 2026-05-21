import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `<div class="iwx-dark min-h-screen"><router-outlet /></div>`
})
export class App {}
