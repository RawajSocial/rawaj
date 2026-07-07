import { Component, input, output } from '@angular/core';
import { GenType, TYPE_CFG } from '../../../model/generated-item.model';

type Counts = { all: number; 'static-ad': number; video: number; text: number };

@Component({
  selector: 'app-media-toolbar',
  standalone: true,
  imports: [],
  templateUrl: './media-toolbar.html',
  styleUrl: './media-toolbar.css',
})
export class MediaToolbar {
  filterType  = input.required<GenType | 'all'>();
  counts      = input.required<Counts>();
  searchQuery = input.required<string>();

  readonly typeCfg = TYPE_CFG;

  filterTypeChange  = output<GenType | 'all'>();
  searchQueryChange = output<string>();
}
